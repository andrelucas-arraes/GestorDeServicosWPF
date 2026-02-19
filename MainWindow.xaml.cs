using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestaoAulas.ViewModels;
using GestaoAulas.Utils;
using System.Linq;
using System;

namespace GestaoAulas
{
    public partial class MainWindow : Window
    {
        public MainWindow(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Inicializa campo de data com hoje
            txtNovaData.Text = DateTime.Now.ToString("dd/MM/yyyy");
        }

        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                if (vm.EditarAulaCommand.CanExecute(null))
                    vm.EditarAulaCommand.Execute(null);
            }
        }

        /// <summary>
        /// Formatação automática de duração (H:MM) ao digitar.
        /// </summary>
        private void Duracao_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Só aceita dígitos
                if (!char.IsDigit(e.Text, 0))
                {
                    e.Handled = true;
                    return;
                }

                string currentText = textBox.Text;
                string digits = new string(currentText.Where(char.IsDigit).ToArray());
                
                // Limita a 4 dígitos (ex: 10:30 = 1030)
                if (digits.Length >= 4)
                {
                    e.Handled = true;
                    return;
                }
                
                digits += e.Text;
                
                // Formata como H:MM
                textBox.Text = FormatarDuracao(digits);
                textBox.CaretIndex = textBox.Text.Length;
                
                e.Handled = true;
            }
        }

        /// <summary>
        /// Trata backspace no campo de duração.
        /// </summary>
        private void Duracao_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (e.Key == Key.Back)
                {
                    string text = textBox.Text;
                    if (string.IsNullOrEmpty(text)) return;

                    string digits = new string(text.Where(char.IsDigit).ToArray());
                    if (digits.Length > 0)
                    {
                        digits = digits.Substring(0, digits.Length - 1);
                    }

                    textBox.Text = FormatarDuracao(digits);
                    textBox.CaretIndex = textBox.Text.Length;
                    e.Handled = true;
                }
                else if (e.Key == Key.Enter)
                {
                    // Valida a data antes de adicionar (Fix #11)
                    if (!FormatUtils.TryParseData(txtNovaData.Text, out _))
                    {
                        GestaoAulas.Views.CustomMessageBox.Show(
                            "Data inválida ou incompleta. Corrija antes de adicionar.",
                            "Validação",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning);
                        txtNovaData.Focus();
                        e.Handled = true;
                        return;
                    }
                    Adicionar_Click(null!, null!);
                    e.Handled = true;
                }
            }
        }

        /// <summary>
        /// Formatação automática de data (DD/MM/AAAA) ao digitar.
        /// </summary>
        private void Data_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!char.IsDigit(e.Text, 0)) { e.Handled = true; return; }
                
                string digits = new string(textBox.Text.Where(char.IsDigit).ToArray());
                if (digits.Length >= 8) { e.Handled = true; return; }

                FormatUtils.MascaraData(textBox);
            }
        }

        private void Data_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textBox && e.Key == Key.Back)
            {
                FormatUtils.MascaraData(textBox, true);
            }
        }

        /// <summary>
        /// Formata dígitos como duração H:MM.
        /// </summary>
        private static string FormatarDuracao(string digits)
        {
            if (string.IsNullOrEmpty(digits)) return "";
            
            // Se tiver 3+ dígitos, formata como H:MM
            if (digits.Length >= 3)
            {
                return digits.Substring(0, digits.Length - 2) + ":" + digits.Substring(digits.Length - 2);
            }
            return digits;
        }

        private void Adicionar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                // Tenta parsear a data do TextBox antes de rodar o comando
                if (FormatUtils.TryParseData(txtNovaData.Text, out DateTime data))
                {
                    vm.NovaData = data;
                }
                
                if (vm.AdicionarAulaCommand.CanExecute(null))
                {
                    vm.AdicionarAulaCommand.Execute(null);
                    // Após adicionar, limpa ou reseta a data (o VM já reseta para hoje, vamos sincronizar)
                    txtNovaData.Text = DateTime.Now.ToString("dd/MM/yyyy");
                }
            }
        }
    }
}
