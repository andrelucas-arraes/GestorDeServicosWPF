using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestaoAulas.Services;
using GestaoAulas.Models;
using GestaoAulas.Utils;
using Microsoft.Win32;

namespace GestaoAulas.Views
{
    /// <summary>
    /// Diálogo de configurações da aplicação.
    /// </summary>
    public partial class ConfiguracoesDialog : Window
    {
        public ConfiguracoesDialog()
        {
            InitializeComponent();
            
            Serilog.Log.Debug("Abrindo diálogo de configurações. ValorHoraAtual={Valor}", 
                GestaoAulas.Models.Aula.ValorHoraAula);
            
            CarregarConfiguracoes();

            // Formatação automática ao sair do campo (mantém compatibilidade)
            txtValorHora.LostFocus += (s, e) => 
            {
                FormatarCampoValor();
            };
        }

        /// <summary>
        /// Formata o campo de valor monetário.
        /// </summary>
        private void FormatarCampoValor()
        {
            string texto = txtValorHora.Text.Replace("R$", "").Replace(".", ",").Trim();
            if (decimal.TryParse(texto, NumberStyles.Currency, new CultureInfo("pt-BR"), out decimal valor))
            {
                txtValorHora.Text = valor.ToString("N2", new CultureInfo("pt-BR"));
            }
        }

        /// <summary>
        /// Formatação automática de valor ao digitar.
        /// </summary>
        private void ValorHora_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Aceita apenas dígitos e vírgula
                char c = e.Text[0];
                if (!char.IsDigit(c) && c != ',')
                {
                    e.Handled = true;
                    return;
                }
                
                // Só permite uma vírgula
                if (c == ',' && textBox.Text.Contains(','))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        /// <summary>
        /// Trata backspace no campo de valor.
        /// </summary>
        private void ValorHora_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // Permite navegação normal, apenas formata ao sair do campo
        }

        /// <summary>
        /// Carrega as configurações atuais.
        /// </summary>
        private void CarregarConfiguracoes()
        {
            // Valor da hora-aula
            txtValorHora.Text = Aula.ValorHoraAula.ToString("N2", new CultureInfo("pt-BR"));

            // Info do banco
            var infoBanco = BackupManager.Instance.ObterInfoBanco();
            if (infoBanco.TamanhoBytes > 0)
            {
                string tamanho = FormatarTamanho(infoBanco.TamanhoBytes);
                txtInfoBanco.Text = $"Banco de dados: {tamanho}";
                txtUltimoBackup.Text = $"Última modificação: {infoBanco.UltimaModificacao:dd/MM/yyyy HH:mm}";
            }

            // Backup externo
            if (!string.IsNullOrEmpty(BackupManager.Instance.CaminhoBackupExterno))
            {
                txtCaminhoExterno.Text = BackupManager.Instance.CaminhoBackupExterno;
            }
        }

        /// <summary>
        /// Faz backup imediato.
        /// </summary>
        private async void FazerBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Cursor = Cursors.Wait;
                Serilog.Log.Information("Iniciando backup manual via interface");
                
                bool sucesso = await BackupManager.Instance.CriarBackupAsync();
                
                if (sucesso)
                {
                    CustomMessageBox.Show("Backup realizado com sucesso!", 
                        "Backup", MessageBoxButton.OK, MessageBoxImage.Information);
                    CarregarConfiguracoes();
                }
                else
                {
                    CustomMessageBox.Show("Não foi possível criar o backup. Verifique os logs para mais detalhes.", 
                        "Backup", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao processar backup manual");
                CustomMessageBox.Show($"Erro ao criar backup: {ex.Message}", 
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        /// <summary>
        /// Restaura um backup selecionado.
        /// </summary>
        private async void RestaurarBackup_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var backups = BackupManager.Instance.ListarBackups();
                
                if (backups.Length == 0)
                {
                    CustomMessageBox.Show("Nenhum backup disponível para restauração.", 
                        "Restaurar", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var dialog = new OpenFileDialog
                {
                    Title = "Selecionar Backup para Restaurar",
                    Filter = "Arquivos de Banco de Dados (*.db)|*.db",
                    InitialDirectory = BackupManager.Instance.PastaBackup
                };

                if (dialog.ShowDialog() == true)
                {
                    var resultado = CustomMessageBox.Show(
                        "Tem certeza que deseja restaurar este backup?\n\nATENÇÃO: Os dados atuais serão substituídos permanentemente.",
                        "Confirmar Restauração",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        Cursor = Cursors.Wait;
                        Serilog.Log.Information("Iniciando restauração de backup via interface: {File}", dialog.FileName);
                        
                        bool sucesso = await BackupManager.Instance.RestaurarBackupAsync(dialog.FileName);
                        
                        if (sucesso)
                        {
                            CustomMessageBox.Show("Restauração concluída com sucesso!\n\nA aplicação deve ser reiniciada para carregar os novos dados.",
                                "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            CustomMessageBox.Show("Falha ao restaurar backup. O arquivo pode estar corrompido ou em uso.",
                                "Erro na Restauração", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Falha durante o processo de restauração");
                CustomMessageBox.Show($"Erro crítico na restauração: {ex.Message}",
                    "Erro Fatal", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

        /// <summary>
        /// Seleciona pasta para backup externo.
        /// </summary>
        private void SelecionarPastaExterna_Click(object sender, RoutedEventArgs e)
        {
            using var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "Selecione uma pasta para backup externo",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                txtCaminhoExterno.Text = dialog.SelectedPath;
            }
        }

        /// <summary>
        /// Salva as configurações.
        /// </summary>
        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Valida e salva valor da hora-aula
                string valorTexto = txtValorHora.Text.Replace("R$", "").Replace(".", ",").Trim();
                if (!decimal.TryParse(valorTexto, NumberStyles.Currency, new CultureInfo("pt-BR"), out decimal valor))
                {
                    CustomMessageBox.Show("Valor da hora inválido! Por favor, informe um valor numérico válido.",
                        "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtValorHora.Focus();
                    return;
                }
                
                // Validação adicional para valor razoável
                if (valor <= 0 || valor > 10000)
                {
                    CustomMessageBox.Show("O valor da hora deve ser maior que zero e menor que R$ 10.000,00.",
                        "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtValorHora.Focus();
                    return;
                }
                
                Aula.ValorHoraAula = valor;

                // Salva caminho de backup externo (permite limpar se vazio)
                BackupManager.Instance.CaminhoBackupExterno = txtCaminhoExterno.Text?.Trim() ?? "";
                
                // Salva todas as configurações (incluindo valor hora e caminho)
                BackupManager.Instance.SalvarConfiguracoes();

                Serilog.Log.Information("Configurações salvas com sucesso. Novo ValorHora={Valor}", valor);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao salvar configurações");
                CustomMessageBox.Show($"Erro ao salvar: {ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cancela e fecha.
        /// </summary>
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        /// <summary>
        /// Formata tamanho em bytes para exibição.
        /// </summary>
        private static string FormatarTamanho(long bytes)
        {
            string[] sufixos = { "B", "KB", "MB", "GB" };
            int ordem = 0;
            double tamanho = bytes;

            while (tamanho >= 1024 && ordem < sufixos.Length - 1)
            {
                ordem++;
                tamanho /= 1024;
            }

            return $"{tamanho:N2} {sufixos[ordem]}";
        }
    }
}
