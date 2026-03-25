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

            Loaded += async (s, e) =>
            {
                try { await viewModel.InitializeAsync(); }
                catch (Exception)
                {
                    GestaoAulas.Views.CustomMessageBox.Show(
                        "Erro ao carregar dados iniciais. Reinicie o aplicativo.",
                        "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
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
        /// Formatação automática de data (DD/MM/AAAA) ao digitar.
        /// </summary>
        private void Data_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!char.IsDigit(e.Text, 0)) { e.Handled = true; return; }
                
                string digits = new string(textBox.Text.Where(char.IsDigit).ToArray());
                if (digits.Length >= 8) { e.Handled = true; return; }

                digits += e.Text;
                textBox.Text = FormatarData(digits);
                textBox.CaretIndex = textBox.Text.Length;
                
                e.Handled = true;
            }
        }

        private void Data_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textBox && e.Key == Key.Back)
            {
                FormatUtils.MascaraData(textBox, true);
            }
            else if (e.Key == Key.Enter)
            {
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

        /// <summary>
        /// Formata dígitos como data DD/MM/AAAA.
        /// </summary>
        private static string FormatarData(string digits)
        {
            if (string.IsNullOrEmpty(digits)) return "";
            
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < digits.Length; i++)
            {
                sb.Append(digits[i]);
                if ((i == 1 || i == 3) && i < digits.Length - 1)
                {
                    sb.Append('/');
                }
            }
            return sb.ToString();
        }

        private void Adicionar_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel vm)
            {
                if (FormatUtils.TryParseData(txtNovaData.Text, out DateTime data))
                {
                    vm.NovaData = data;
                }
                
                if (vm.AdicionarAulaCommand.CanExecute(null))
                {
                    vm.AdicionarAulaCommand.Execute(null);
                    txtNovaData.Text = DateTime.Now.ToString("dd/MM/yyyy");
                }
            }
        }
    }
}
