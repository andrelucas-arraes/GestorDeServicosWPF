using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using GestaoAulas.Models;
using GestaoAulas.Utils;
using System.Linq;

namespace GestaoAulas.Views
{
    /// <summary>
    /// Diálogo para edição/criação de aulas.
    /// </summary>
    public partial class AulaDialog : Window
    {
        private readonly Aula _aulaOriginal;
        private readonly Aula _aulaEdicao;
        private readonly bool _isNova;


        public AulaDialog(Aula? aula = null)
        {
            InitializeComponent();

            _isNova = aula == null;
            _aulaOriginal = aula ?? Aula.CriarNova();
            _aulaEdicao = _isNova ? _aulaOriginal : _aulaOriginal.Clone();

            DataContext = _aulaEdicao;
            Title = _isNova ? "Nova Aula" : "Editar Aula";

            // Inicialização dos campos
            Loaded += (s, e) =>
            {
                // Configura eventos de teclado PRIMEIRO
                txtData.PreviewTextInput += TxtData_PreviewTextInput;
                // txtData.PreviewKeyDown += TxtData_PreviewKeyDown; // Removido para evitar bug de backspace
                
                // Inicializa o campo de data
                txtData.Text = _aulaEdicao.Data.ToString("dd/MM/yyyy");

                txtDuracao.Text = _aulaEdicao.DuracaoFormatada;
                txtDiaSemana.Text = _aulaEdicao.DiaSemana;
                
                txtNomeAula.Focus();

                // Recalcula valor apenas ao sair do campo para evitar pulos de cursor e loops de formatação
                txtDuracao.LostFocus += (s, ev) => ReatualizarValorRealTime();
                txtValorHora.LostFocus += (s, ev) => ReatualizarValorRealTime();
            };
        }

        private void ReatualizarValorRealTime()
        {
            // Tenta obter os valores atuais sem disparar novos ciclos de atualização desnecessários
            bool duraOk = FormatUtils.TryParseDuracao(txtDuracao.Text, out double duracao);
            bool valorOk = FormatUtils.TryParseDecimal(txtValorHora.Text, out decimal vh);

            if (duraOk) _aulaEdicao.Duracao = duracao;
            if (valorOk) _aulaEdicao.ValorHora = vh;

            if (duraOk || valorOk)
            {
                _aulaEdicao.RecalcularValor();
                
                // Atualiza a visualização formatada se necessário (pois o binding pode não atualizar se não for TwoWay/PropertyChanged)
                if (duraOk) txtDuracao.Text = _aulaEdicao.DuracaoFormatada;
            }
        }

        /// <summary>
        /// Processa entrada de texto no campo de data com máscara automática.
        /// </summary>
        private void TxtData_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!char.IsDigit(e.Text, 0)) { e.Handled = true; return; }
                
                string digits = new string(textBox.Text.Where(char.IsDigit).ToArray());
                if (digits.Length >= 8) { e.Handled = true; return; }

                FormatUtils.MascaraData(textBox);
                
                // Tenta atualizar o dia da semana em tempo real se a data estiver completa
                if (FormatUtils.TryParseData(textBox.Text, out DateTime data))
                {
                    txtDiaSemana.Text = ObterDiaSemanaBreve(data);
                }
            }
        }

        // Removido TxtData_PreviewKeyDown para corrigir bug de dupla exclusão no Backspace
        /*
        private void TxtData_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is TextBox textBox && e.Key == Key.Back)
            {
                FormatUtils.MascaraData(textBox, true);
            }
        }
        */

        private string ObterDiaSemanaBreve(DateTime data)
        {
            return data.DayOfWeek switch
            {
                DayOfWeek.Sunday => "Dom",
                DayOfWeek.Monday => "Seg",
                DayOfWeek.Tuesday => "Ter",
                DayOfWeek.Wednesday => "Qua",
                DayOfWeek.Thursday => "Qui",
                DayOfWeek.Friday => "Sex",
                DayOfWeek.Saturday => "Sáb",
                _ => ""
            };
        }


        /// <summary>
        /// Intercepta entrada de texto no campo de duração.
        /// </summary>
        private void Duracao_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!char.IsDigit(e.Text, 0))
                {
                    e.Handled = true;
                    return;
                }

                // Calcula como o texto ficará após a inserção (respeitando seleção)
                string fullText = textBox.Text;
                string newPart = e.Text;
                
                // Remove o trecho selecionado
                if (textBox.SelectionLength > 0)
                {
                    fullText = fullText.Remove(textBox.SelectionStart, textBox.SelectionLength);
                }
                
                // Insere o novo texto na posição correta
                string finalPredicted = fullText.Insert(textBox.CaretIndex, newPart);
                
                // Extrai apenas os dígitos para formatar
                string digits = new string(finalPredicted.Where(char.IsDigit).ToArray());
                
                if (digits.Length >= 5) // Limite H:MM (ex: 99:59)
                {
                    e.Handled = true;
                    return;
                }
                
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
            if (sender is TextBox textBox && e.Key == Key.Back)
            {
                string text = textBox.Text;
                if (string.IsNullOrEmpty(text)) return;

                if (textBox.SelectionLength > 0)
                {
                    // Remove a seleção
                    text = text.Remove(textBox.SelectionStart, textBox.SelectionLength);
                }
                else if (textBox.CaretIndex > 0)
                {
                    // Remove o caractere anterior
                    text = text.Remove(textBox.CaretIndex - 1, 1);
                }

                string digits = new string(text.Where(char.IsDigit).ToArray());
                textBox.Text = FormatarDuracao(digits);
                textBox.CaretIndex = textBox.Text.Length;
                e.Handled = true;
            }
        }

        /// <summary>
        /// Formata dígitos como duração H:MM.
        /// </summary>
        private string FormatarDuracao(string digits)
        {
            if (string.IsNullOrEmpty(digits)) return "";
            
            if (digits.Length >= 3)
            {
                return digits.Substring(0, digits.Length - 2) + ":" + digits.Substring(digits.Length - 2);
            }
            return digits;
        }

        /// <summary>
        /// Salva a aula.
        /// </summary>
        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Valida nome
                if (string.IsNullOrWhiteSpace(txtNomeAula.Text))
                {
                    CustomMessageBox.Show("Por favor, informe o nome do aluno ou descrição da aula.",
                        "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtNomeAula.Focus();
                    return;
                }

                // Valida data
                if (!FormatUtils.TryParseData(txtData.Text, out DateTime data))
                {
                    CustomMessageBox.Show("Por favor, informe uma data válida no formato DD/MM/AAAA.",
                        "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtData.Focus();
                    return;
                }


                // Valida data futura
                if (data.Date > DateTime.Now.Date)
                {
                    var result = CustomMessageBox.Show("A data informada é futura. Deseja continuar?",
                        "Data Futura", MessageBoxButton.YesNo, MessageBoxImage.Question);
                     
                    if (result == MessageBoxResult.No)
                    {
                        txtData.Focus();
                        return;
                    }
                }

                // Valida duração apenas se for Aula
                double duracao = 0;
                if (_aulaEdicao.Categoria == "Aula")
                {
                    if (!FormatUtils.TryParseDuracao(txtDuracao.Text, out duracao))
                    {
                        CustomMessageBox.Show("Por favor, informe uma duração válida (ex: 1:30 ou 1.5).",
                            "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtDuracao.Focus();
                        return;
                    }

                    if (duracao > 1000)
                    {
                        CustomMessageBox.Show("A duração informada parece excessiva. Por favor, verifique.",
                            "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtDuracao.Focus();
                        return;
                    }
                }

                // Atualiza modelo de edição
                _aulaEdicao.Data = data;
                _aulaEdicao.NomeAula = txtNomeAula.Text.Trim();
                
                if (_aulaEdicao.Categoria == "Aula")
                {
                    _aulaEdicao.Duracao = duracao;

                    // Pega valor hora do campo (Pode ter sido editado)
                    if (FormatUtils.TryParseDecimal(txtValorHora.Text, out decimal vh))
                    {
                        _aulaEdicao.ValorHora = vh;
                    }

                    // Proteção contra valores irreais/erros de digitação
                    if (_aulaEdicao.ValorHora > 1000000)
                    {
                        CustomMessageBox.Show("O valor da hora informado parece excessivo. Por favor, verifique.",
                            "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                        txtValorHora.Focus();
                        return;
                    }
                }

                // Se NÃO for aula (Serviço, etc), lemos o valor manualmente do campo de texto
                // Isso previne bugs onde o binding não atualizou o ViewModel (ex: LostFocus não disparou)
                if (_aulaEdicao.Categoria != "Aula")
                {
                     if (FormatUtils.TryParseDecimal(txtValor.Text, out decimal valManual))
                     {
                         _aulaEdicao.Valor = valManual;
                     }
                }

                // Sempre recalcula o valor baseado na nova duração e valor hora (se for Aula)
                _aulaEdicao.RecalcularValor();

                // Copia valores de volta para o objeto original se for edição
                if (!_isNova)
                {
                    _aulaOriginal.Data = _aulaEdicao.Data;
                    _aulaOriginal.DiaSemana = _aulaEdicao.DiaSemana;
                    _aulaOriginal.NomeAula = _aulaEdicao.NomeAula;
                    _aulaOriginal.Duracao = _aulaEdicao.Duracao;
                    _aulaOriginal.ValorHora = _aulaEdicao.ValorHora;
                    _aulaOriginal.Valor = _aulaEdicao.Valor;
                    _aulaOriginal.Status = _aulaEdicao.Status;
                    _aulaOriginal.Categoria = _aulaEdicao.Categoria;
                    _aulaOriginal.DataAtualizacao = DateTime.Now;
                }

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
