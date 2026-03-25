using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestaoAulas.Services;
using GestaoAulas.Models;
using GestaoAulas.Repositories;
using GestaoAulas.Utils;
using Microsoft.Win32;

namespace GestaoAulas.Views
{
    /// <summary>
    /// Diálogo de configurações da aplicação.
    /// </summary>
    public partial class ConfiguracoesDialog : Window
    {
        private readonly IAulaRepository _repository;

        public ConfiguracoesDialog()
        {
            InitializeComponent();
            
            // Obtém o repository via DI
            _repository = (IAulaRepository)App.AppHost!.Services.GetService(typeof(IAulaRepository))!;
            
            Serilog.Log.Debug("Abrindo diálogo de configurações.");
            
            CarregarConfiguracoes();
            CarregarCategoriasAsync();
        }

        private async void CarregarCategoriasAsync()
        {
            try
            {
                var categorias = await _repository.ObterCategoriasAsync();
                lstCategorias.Items.Clear();
                foreach (var cat in categorias)
                {
                    lstCategorias.Items.Add(cat);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao carregar categorias no diálogo de configurações");
            }
        }

        private async void AdicionarCategoria_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var novaCategoria = txtNovaCategoria.Text?.Trim();
                if (string.IsNullOrWhiteSpace(novaCategoria))
                {
                    CustomMessageBox.Show("Digite o nome da nova categoria.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNovaCategoria.Focus();
                    return;
                }

                if (novaCategoria.Length < 2)
                {
                    CustomMessageBox.Show("O nome da categoria deve ter pelo menos 2 caracteres.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNovaCategoria.Focus();
                    return;
                }

                // Verifica se já existe
                foreach (var item in lstCategorias.Items)
                {
                    if (item.ToString()!.Equals(novaCategoria, StringComparison.OrdinalIgnoreCase))
                    {
                        CustomMessageBox.Show("Essa categoria já existe.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                await _repository.AdicionarCategoriaAsync(novaCategoria);
                lstCategorias.Items.Add(novaCategoria);
                txtNovaCategoria.Text = "";

                CustomMessageBox.Show($"Categoria '{novaCategoria}' adicionada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao adicionar categoria");
                CustomMessageBox.Show($"Erro ao adicionar categoria: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void RemoverCategoria_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lstCategorias.SelectedItem == null)
                {
                    CustomMessageBox.Show("Selecione uma categoria para remover.", "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var categoriaSelecionada = lstCategorias.SelectedItem.ToString()!;
                
                // Categorias protegidas
                var protegidas = new[] { "Aula", "Serviço" };
                if (protegidas.Contains(categoriaSelecionada))
                {
                    CustomMessageBox.Show($"A categoria '{categoriaSelecionada}' é uma categoria padrão e não pode ser removida.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var resultado = CustomMessageBox.Show(
                    $"Deseja remover a categoria '{categoriaSelecionada}'?\n\nRegistros existentes com esta categoria NÃO serão afetados.",
                    "Confirmar Remoção",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    await _repository.RemoverCategoriaAsync(categoriaSelecionada);
                    lstCategorias.Items.Remove(categoriaSelecionada);
                    
                    CustomMessageBox.Show($"Categoria '{categoriaSelecionada}' removida.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao remover categoria");
                CustomMessageBox.Show($"Erro ao remover categoria: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CarregarConfiguracoes()
        {
            if (!BackupManager.IsInitialized) return;
            
            var infoBanco = BackupManager.Instance.ObterInfoBanco();
            if (infoBanco.TamanhoBytes > 0)
            {
                string tamanho = FormatarTamanho(infoBanco.TamanhoBytes);
                txtInfoBanco.Text = $"Banco de dados: {tamanho}";
                txtUltimoBackup.Text = $"Última modificação: {infoBanco.UltimaModificacao:dd/MM/yyyy HH:mm}";
            }

            if (!string.IsNullOrEmpty(BackupManager.Instance.CaminhoBackupExterno))
            {
                txtCaminhoExterno.Text = BackupManager.Instance.CaminhoBackupExterno;
            }
        }

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
                    CustomMessageBox.Show("Não foi possível criar o backup. Verifique os logs.", 
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
                        Serilog.Log.Information("Restaurando backup: {File}", dialog.FileName);
                        
                        bool sucesso = await BackupManager.Instance.RestaurarBackupAsync(dialog.FileName);
                        
                        if (sucesso)
                        {
                            CustomMessageBox.Show("Restauração concluída!\n\nA aplicação será reiniciada.",
                                "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            CustomMessageBox.Show("Falha ao restaurar backup. O arquivo pode estar corrompido.",
                                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Falha durante restauração");
                CustomMessageBox.Show($"Erro crítico na restauração: {ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                Cursor = Cursors.Arrow;
            }
        }

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
                
                // Salva diretamente
                BackupManager.Instance.CaminhoBackupExterno = dialog.SelectedPath;
                BackupManager.Instance.SalvarConfiguracoes();
            }
        }

        private void Fechar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

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
