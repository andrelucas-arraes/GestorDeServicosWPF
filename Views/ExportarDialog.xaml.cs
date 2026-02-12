using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using GestaoAulas.Export;
using GestaoAulas.Models;
using GestaoAulas.Utils;
using Microsoft.Win32;

namespace GestaoAulas.Views
{
    /// <summary>
    /// Diálogo para exportação de dados.
    /// </summary>
    public partial class ExportarDialog : Window
    {
        private readonly List<Aula> _aulas;
        private readonly int? _mes;
        private readonly int? _ano;

        public ExportarDialog(List<Aula> aulas, int? mes = null, int? ano = null)
        {
            InitializeComponent();

            _aulas = aulas;
            _mes = mes;
            _ano = ano;

            Serilog.Log.Debug("Abrindo diálogo de exportação. TotalAulas={Count}, Mes={Mes}, Ano={Ano}", 
                aulas.Count, mes, ano);

            txtInfo.Text = $"Exportar {aulas.Count} aula(s) do período selecionado";

            // Define caminho padrão na pasta de exports
            string extensao = "xlsx";
            string nomeArquivo = ExportManager.GerarNomeArquivo(mes, ano, extensao);
            
            // Revertido para pasta local conforme solicitado
            string exportsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports");
            if (!Directory.Exists(exportsPath)) try { Directory.CreateDirectory(exportsPath); } catch { }

            txtCaminho.Text = Path.Combine(exportsPath, nomeArquivo);
        }

        /// <summary>
        /// Abre diálogo para selecionar local.
        /// </summary>
        private void Procurar_Click(object sender, RoutedEventArgs e)
        {
            string extensao = rbExcel.IsChecked == true ? "xlsx" : "csv";
            string filtro = rbExcel.IsChecked == true 
                ? "Planilha Excel (*.xlsx)|*.xlsx" 
                : "Arquivo CSV (*.csv)|*.csv";

            Serilog.Log.Debug("Abrindo seleção de arquivo. Formato={Formato}", extensao.ToUpper());

            string exportsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports");
            // Sem validação try/catch aqui, o FileDialog trata se não existir

            var dialog = new SaveFileDialog
            {
                Title = "Salvar Exportação",
                Filter = filtro,
                FileName = ExportManager.GerarNomeArquivo(_mes, _ano, extensao),
                InitialDirectory = exportsPath
            };

            if (dialog.ShowDialog() == true)
            {
                txtCaminho.Text = dialog.FileName;
                Serilog.Log.Debug("Caminho selecionado: {Path}", dialog.FileName);
            }
        }

        /// <summary>
        /// Executa a exportação de forma assíncrona.
        /// </summary>
        private async void Exportar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(txtCaminho.Text))
                {
                    CustomMessageBox.Show(this, "Por favor, selecione o local para salvar.",
                        "Exportação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (_aulas.Count == 0)
                {
                    Serilog.Log.Warning("Tentativa de exportar sem dados");
                    CustomMessageBox.Show(this, "Não há dados para exportar.",
                        "Exportação", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // UI Feedback
                Cursor = System.Windows.Input.Cursors.Wait;
                btnExportar.IsEnabled = false;

                bool sucesso;
                string caminho = txtCaminho.Text;
                string formato = rbExcel.IsChecked == true ? "Excel" : "CSV";
                
                Serilog.Log.Information("Iniciando exportação assíncrona. Formato={Formato}, Registros={Count}, Arquivo={Path}", 
                    formato, _aulas.Count, caminho);

                // Ajusta extensão se necessário
                if (rbExcel.IsChecked == true)
                {
                    if (!caminho.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                    {
                        caminho = Path.ChangeExtension(caminho, ".xlsx");
                    }
                    sucesso = await ExportManager.ExportarParaExcelAsync(_aulas, caminho).ConfigureAwait(true);
                }
                else
                {
                    if (!caminho.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
                    {
                        caminho = Path.ChangeExtension(caminho, ".csv");
                    }
                    sucesso = await ExportManager.ExportarParaCsvAsync(_aulas, caminho).ConfigureAwait(true);
                }

                // Volta UI ao normal
                Cursor = System.Windows.Input.Cursors.Arrow;
                btnExportar.IsEnabled = true;

                if (sucesso)
                {
                    Serilog.Log.Information("Exportação concluída com sucesso. Formato={Formato}, Arquivo={Path}", formato, caminho);
                    
                    var resultado = CustomMessageBox.Show(this, 
                        $"Exportação concluída com sucesso!\n\nArquivo: {caminho}\n\nDeseja abrir o arquivo?",
                        "Exportação",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        Serilog.Log.Debug("Abrindo arquivo exportado: {Path}", caminho);
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = caminho,
                            UseShellExecute = true
                        });
                    }

                    DialogResult = true;
                    Close();
                }
                else
                {
                    Serilog.Log.Warning("Exportação falhou. Formato={Formato}, Arquivo={Path}", formato, caminho);
                    CustomMessageBox.Show(this, "Não foi possível exportar os dados.",
                        "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                Cursor = System.Windows.Input.Cursors.Arrow;
                btnExportar.IsEnabled = true;
                Serilog.Log.Error(ex, "Erro ao exportar dados. Caminho={Path}", txtCaminho.Text);
                CustomMessageBox.Show(this, $"Erro ao exportar: {ex.Message}",
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
    }
}
