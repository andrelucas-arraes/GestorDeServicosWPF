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
                // Inicializa o campo de data
                dtData.SelectedDate = _aulaEdicao.Data;

                // Atualiza o dia da semana ao mudar a data
                dtData.SelectedDateChanged += (sender, args) =>
                {
                    if (dtData.SelectedDate.HasValue)
                    {
                        txtDiaSemana.Text = ObterDiaSemanaBreve(dtData.SelectedDate.Value);
                        // Atualiza o model imediatamente (Fix #18)
                        _aulaEdicao.Data = dtData.SelectedDate.Value;
                    }
                };

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

                // Se for Aula, recalcula valor com base em duração e valor/hora
                // MAS preserva valor manual se o usuário digitou um diferente (Fix #9)
                if (_aulaEdicao.Categoria == "Aula")
                {
                    decimal valorAntes = _aulaEdicao.Valor;
                    _aulaEdicao.RecalcularValor();
                    
                    // Se o campo txtValor tiver valor diferente do recalculado, o usuário editou manualmente
                    if (FormatUtils.TryParseDecimal(txtValor.Text, out decimal valDigitado) && valDigitado != _aulaEdicao.Valor && valDigitado != valorAntes)
                    {
                        // O usuário alterou o valor manualmente — mantém o digitado
                        _aulaEdicao.Valor = valDigitado;
                    }
                }

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
