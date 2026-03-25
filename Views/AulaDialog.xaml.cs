using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestaoAulas.Models;
using GestaoAulas.Repositories;
using GestaoAulas.Utils;
using System.Linq;

namespace GestaoAulas.Views
{
    /// <summary>
    /// Diálogo para edição/criação de aulas.
    /// </summary>
    public partial class AulaDialog : Window
    {
        private readonly Aula _aula;
        private readonly bool _isNova;

        public AulaDialog(Aula? aula = null)
        {
            InitializeComponent();

            _isNova = aula == null;
            _aula = aula ?? Aula.CriarNova();

            DataContext = _aula;
            Title = _isNova ? "Novo Registro" : "Editar Registro";

            Loaded += async (s, e) =>
            {
                // Carrega categorias do banco
                try
                {
                    var repository = (IAulaRepository)App.AppHost!.Services.GetService(typeof(IAulaRepository))!;
                    var categorias = await repository.ObterCategoriasAsync();
                    
                    cmbCategoria.Items.Clear();
                    foreach (var cat in categorias)
                    {
                        cmbCategoria.Items.Add(new ComboBoxItem { Content = cat });
                    }

                    // Seleciona a categoria atual
                    cmbCategoria.SelectedValue = _aula.Categoria;
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Erro ao carregar categorias no diálogo de aula");
                    // Fallback
                    var categoriasPadrao = new[] { "Aula", "Serviço", "Freelance", "Manutenção", "Outro" };
                    cmbCategoria.Items.Clear();
                    foreach (var cat in categoriasPadrao)
                    {
                        cmbCategoria.Items.Add(new ComboBoxItem { Content = cat });
                    }
                    cmbCategoria.SelectedValue = _aula.Categoria;
                }

                // Inicializa campo de data
                dtData.SelectedDate = _aula.Data;

                // Atualiza dia da semana ao mudar data
                dtData.SelectedDateChanged += (sender, args) =>
                {
                    if (dtData.SelectedDate.HasValue)
                    {
                        txtDiaSemana.Text = Aula.ObterDiaSemana(dtData.SelectedDate.Value);
                        _aula.Data = dtData.SelectedDate.Value;
                    }
                };

                txtDiaSemana.Text = _aula.DiaSemana;
                txtNomeAula.Focus();
            };
        }

        /// <summary>
        /// Salva o registro.
        /// </summary>
        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Valida nome
                if (string.IsNullOrWhiteSpace(txtNomeAula.Text))
                {
                    CustomMessageBox.Show("Por favor, informe o nome/descrição.",
                        "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNomeAula.Focus();
                    return;
                }

                // Valida data
                if (!dtData.SelectedDate.HasValue)
                {
                    CustomMessageBox.Show("Por favor, selecione uma data válida.",
                        "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    dtData.Focus();
                    return;
                }
                DateTime data = dtData.SelectedDate.Value;

                // Valida data futura
                if (data.Date > DateTime.Now.Date)
                {
                    var result = CustomMessageBox.Show("A data informada é futura. Deseja continuar?",
                        "Data Futura", MessageBoxButton.YesNo, MessageBoxImage.Question);
                     
                    if (result == MessageBoxResult.No)
                    {
                        dtData.Focus();
                        return;
                    }
                }

                // Valida valor
                if (!FormatUtils.TryParseDecimal(txtValor.Text, out decimal valorFinal))
                {
                    CustomMessageBox.Show("Por favor, informe um valor válido (ex: 50,00).",
                        "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtValor.Focus();
                    return;
                }

                if (valorFinal < 0)
                {
                    CustomMessageBox.Show("O valor não pode ser negativo.",
                        "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtValor.Focus();
                    return;
                }

                // Atualiza modelo
                _aula.Data = data;
                _aula.NomeAula = txtNomeAula.Text.Trim();
                _aula.Valor = valorFinal;
                _aula.DataAtualizacao = DateTime.Now;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                CustomMessageBox.Show($"Erro ao salvar: {ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Cancela e fecha o diálogo.
        /// </summary>
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
