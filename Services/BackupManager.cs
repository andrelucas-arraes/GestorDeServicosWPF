using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GestaoAulas.Repositories;

namespace GestaoAulas.Services
{
    /// <summary>
    /// Gerenciador de backups do banco de dados.
    /// </summary>
    public class BackupManager
    {
        // instância estática para compatibilidade com ConfiguracoesDialog existente
        public static BackupManager Instance { get; private set; } = null!;

        public int MaxBackups { get; set; } = 50; // Aumentado para 50 para maior segurança
        public string? CaminhoBackupExterno { get; set; }

        private readonly string _databasePath;
        private readonly string _backupsPath;
        private readonly string _appDataPath;
        private readonly string _configFile;

        // Dependência do repositório para fazer o backup seguro (VACUUM)
        private readonly IAulaRepository _repository;

        public BackupManager(IAulaRepository repository)
        {
            _repository = repository;
            
            // Configura caminhos para AppData (Mais seguro e correto para Windows)
            // Configura caminhos. Em DEBUG usa DevData, RELEASE usa AppData.
#if DEBUG
            _appDataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DevData");
#else
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _appDataPath = Path.Combine(appData, "GestorDeServicos");
#endif
            
            if (!Directory.Exists(_appDataPath))
                Directory.CreateDirectory(_appDataPath);

            _databasePath = Path.Combine(_appDataPath, "aulas.db");
            _backupsPath = Path.Combine(_appDataPath, "backups");
            _configFile = Path.Combine(_appDataPath, "backup_config.txt");

            CarregarConfiguracoes();

            // Seta a instância estática para compatibilidade
            Instance = this;
        }

        public async Task<bool> CriarBackupAsync()
        {
            try
            {
                Serilog.Log.Debug("Iniciando criação de backup. DatabasePath={Path}", _databasePath);
                
                if (!File.Exists(_databasePath))
                {
                    Serilog.Log.Warning("Banco de dados não encontrado para backup. Path={Path}", _databasePath);
                    return false;
                }

                // Garante que a pasta de backups existe
                if (!Directory.Exists(_backupsPath))
                {
                    Directory.CreateDirectory(_backupsPath);
                }

                string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                string nomeBackup = $"aulas_backup_{timestamp}.db";
                string caminhoBackup = Path.Combine(_backupsPath, nomeBackup);

                Serilog.Log.Debug("Criando backup local em: {BackupPath}", caminhoBackup);
                
                // Usa o repositório para backup (VACUUM INTO) - Operação de banco pesada
                await _repository.BackupDatabaseAsync(caminhoBackup).ConfigureAwait(false);

                // Limpa isoladamente em background com tratamento de erro
                _ = Task.Run(() =>
                {
                    try
                    {
                        LimparBackupsAntigos();
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex, "Erro ao limpar backups antigos em background");
                    }
                });

                // Cópia para externo de forma assíncrona
                if (!string.IsNullOrEmpty(CaminhoBackupExterno))
                {
                    await CopiarParaExternoAsync(caminhoBackup, nomeBackup).ConfigureAwait(false);
                }

                Serilog.Log.Information("Processo de backup concluído com sucesso: {BackupPath}", caminhoBackup);
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro fatal durante a criação do backup.");
                return false;
            }
        }

        private async Task CopiarParaExternoAsync(string sourcePath, string fileName)
        {
            try
            {
                if (Directory.Exists(CaminhoBackupExterno))
                {
                    string destPath = Path.Combine(CaminhoBackupExterno, fileName);
                    Serilog.Log.Debug("Copiando backup para pasta externa: {ExternalPath}", destPath);
                    
                    using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
                    using (var destStream = new FileStream(destPath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
                    {
                        await sourceStream.CopyToAsync(destStream).ConfigureAwait(false);
                    }
                    Serilog.Log.Information("Backup externo criado com sucesso.");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Falha na cópia externa do backup.");
            }
        }

        public async Task<bool> RestaurarBackupAsync(string caminhoBackup)
        {
            try
            {
                Serilog.Log.Information("Iniciando restauração segura: {BackupPath}", caminhoBackup);
                
                if (!File.Exists(caminhoBackup)) return false;

                // Backup de segurança antes de qualquer coisa
                await CriarBackupAsync().ConfigureAwait(false);

                Serilog.Log.Debug("Preparando ambiente para restauração (ClearPools, GC)");
                Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
                GC.Collect();
                GC.WaitForPendingFinalizers();
                
                // Estratégia Segura: Renomear Atual -> Copiar Novo -> Reiniciar
                string backupSeguranca = _databasePath + ".old";
                
                if (File.Exists(_databasePath))
                {
                    if (File.Exists(backupSeguranca)) File.Delete(backupSeguranca);
                    File.Move(_databasePath, backupSeguranca);
                }

                File.Copy(caminhoBackup, _databasePath);

                Serilog.Log.Information("Restauração realizada com sucesso. Reiniciando aplicação...");

                // Reiniciar Aplicação
                var appPath = Environment.ProcessPath;
                if (appPath != null)
                {
                    System.Diagnostics.Process.Start(appPath);
                    System.Windows.Application.Current.Shutdown();
                }
                
                return true;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro CRÍTICO ao restaurar backup.");
                
                // Tenta reverter
                try 
                {
                    if (!File.Exists(_databasePath) && File.Exists(_databasePath + ".old"))
                        File.Move(_databasePath + ".old", _databasePath);
                } 
                catch { /* Se falhar aqui, o usuário precisará de suporte manual */ }

                return false;
            }
        }

        public void SalvarConfiguracoes()
        {
            try
            {
                string caminho = CaminhoBackupExterno ?? "";
                string valor = GestaoAulas.Models.Aula.ValorHoraAula.ToString(System.Globalization.CultureInfo.InvariantCulture);
                
                Serilog.Log.Debug("Salvando configurações. Arquivo={Config}", _configFile);
                
                File.WriteAllLines(_configFile, new[] { caminho, valor });
                
                Serilog.Log.Information("Configurações salvas.");
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao salvar configurações.");
            }
        }

        private void CarregarConfiguracoes()
        {
            try
            {
                Serilog.Log.Debug("Carregando configurações. ArquivoConfig={Config}", _configFile);
                
                if (File.Exists(_configFile))
                {
                    string[] linhas = File.ReadAllLines(_configFile);
                    if (linhas.Length > 0 && !string.IsNullOrWhiteSpace(linhas[0]))
                        CaminhoBackupExterno = linhas[0];
                        
                    if (linhas.Length > 1 && decimal.TryParse(linhas[1], System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal valor))
                        GestaoAulas.Models.Aula.ValorHoraAula = valor;
                    
                    Serilog.Log.Information("Configurações carregadas. ValorHora={Valor}, CaminhoExterno={Caminho}", 
                        GestaoAulas.Models.Aula.ValorHoraAula, CaminhoBackupExterno);
                }
                else
                {
                    Serilog.Log.Debug("Arquivo de configurações não existe, usando valores padrão");
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao carregar configurações. ArquivoConfig={Config}", _configFile);
            }
        }

        public string[] ListarBackups()
        {
            try
            {
                if (!Directory.Exists(_backupsPath))
                {
                    Serilog.Log.Debug("Pasta de backups não existe: {Path}", _backupsPath);
                    return Array.Empty<string>();
                }
                
                var backups = Directory.GetFiles(_backupsPath, "aulas_backup_*.db").OrderByDescending(f => f).ToArray();
                Serilog.Log.Debug("Backups encontrados: {Count} arquivos", backups.Length);
                return backups;
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao listar backups. Pasta={Path}", _backupsPath);
                return Array.Empty<string>();
            }
        }

        private void LimparBackupsAntigos()
        {
            try
            {
                var backups = ListarBackups();
                if (backups.Length > MaxBackups)
                {
                    var backupsParaExcluir = backups.Skip(MaxBackups).ToArray();
                    Serilog.Log.Debug("Limpando {Count} backups antigos. MaxBackups={Max}", backupsParaExcluir.Length, MaxBackups);
                    
                    foreach (var backup in backupsParaExcluir)
                    {
                        try
                        {
                            File.Delete(backup);
                            Serilog.Log.Debug("Backup antigo removido: {Path}", backup);
                        }
                        catch (Exception ex)
                        {
                            Serilog.Log.Warning("Não foi possível excluir o backup antigo {Path}: {Message}", backup, ex.Message);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao limpar backups antigos. Pasta={Path}", _backupsPath);
            }
        }

        public (long TamanhoBytes, DateTime UltimaModificacao) ObterInfoBanco()
        {
             try
             {
                 if (!File.Exists(_databasePath))
                 {
                     Serilog.Log.Debug("Banco de dados não encontrado: {Path}", _databasePath);
                     return (0, DateTime.MinValue);
                 }
                 
                 var info = new FileInfo(_databasePath);
                 Serilog.Log.Debug("Info do banco: Tamanho={Bytes} bytes, UltimaModificacao={Data}", info.Length, info.LastWriteTime);
                 return (info.Length, info.LastWriteTime);
             }
             catch (Exception ex)
             {
                 Serilog.Log.Error(ex, "Erro ao obter informações do banco. Path={Path}", _databasePath);
                 return (0, DateTime.MinValue);
             }
        }
    }
}
