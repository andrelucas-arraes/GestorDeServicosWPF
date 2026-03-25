using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using GestaoAulas.Models;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace GestaoAulas.Views
{
    /// <summary>
    /// Diálogo de análises e gráficos baseados nos registros.
    /// </summary>
    public partial class AnalisesDialog : Window
    {
        private readonly List<Aula> _aulas;
        private static readonly CultureInfo _culturePtBr = new("pt-BR");

        // Paleta de cores consistente
        private static readonly SKColor CorVerde = new(34, 197, 94);    // Pago
        private static readonly SKColor CorLaranja = new(249, 115, 22); // Pendente
        private static readonly SKColor CorAzul = new(59, 130, 246);    // Receita
        private static readonly SKColor CorRoxo = new(167, 139, 250);   // Destaques
        private static readonly SKColor CorTextoEixo = new(148, 163, 184);

        private static readonly SKColor[] CoresCategorias = new[]
        {
            new SKColor(99, 102, 241),   // Indigo
            new SKColor(16, 185, 129),   // Verde
            new SKColor(249, 115, 22),   // Laranja
            new SKColor(167, 139, 250),  // Roxo
            new SKColor(236, 72, 153),   // Rosa
            new SKColor(234, 179, 8),    // Amarelo
            new SKColor(20, 184, 166),   // Teal
            new SKColor(239, 68, 68),    // Vermelho
        };

        public AnalisesDialog(List<Aula> aulas)
        {
            InitializeComponent();
            _aulas = aulas ?? new List<Aula>();

            Loaded += (s, e) =>
            {
                try
                {
                    CarregarResumo();
                    CarregarGraficoReceitaMensal();
                    CarregarGraficoCategoria();
                    CarregarGraficoStatus();
                    CarregarGraficoTopClientes();
                }
                catch (Exception ex)
                {
                    Serilog.Log.Error(ex, "Erro ao carregar análises");
                }
            };
        }

        private void CarregarResumo()
        {
            var totalRegistros = _aulas.Count;
            var receitaTotal = _aulas.Where(a => a.Status == "Pago").Sum(a => a.Valor);
            var totalPendente = _aulas.Where(a => a.Status == "Pendente").Sum(a => a.Valor);
            var ticketMedio = totalRegistros > 0 ? _aulas.Sum(a => a.Valor) / totalRegistros : 0;

            txtTotalRegistros.Text = totalRegistros.ToString();
            txtReceitaTotal.Text = receitaTotal.ToString("C2", _culturePtBr);
            txtTotalPendente.Text = totalPendente.ToString("C2", _culturePtBr);
            txtTicketMedio.Text = ticketMedio.ToString("C2", _culturePtBr);
            txtSubtitulo.Text = $"Analisando {totalRegistros} registro(s)";
        }

        private void CarregarGraficoReceitaMensal()
        {
            try
            {
                var dadosPorMes = _aulas
                    .GroupBy(a => new { a.Data.Year, a.Data.Month })
                    .OrderBy(g => g.Key.Year)
                    .ThenBy(g => g.Key.Month)
                    .Select(g => new
                    {
                        Label = $"{g.Key.Month:D2}/{g.Key.Year}",
                        Valor = (double)g.Sum(a => a.Valor)
                    })
                    .TakeLast(12)
                    .ToList();

                if (dadosPorMes.Count == 0)
                {
                    txtSemDadosReceita.Visibility = Visibility.Visible;
                    chartReceitaMensal.Visibility = Visibility.Collapsed;
                    return;
                }

                chartReceitaMensal.Series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = dadosPorMes.Select(d => d.Valor).ToArray(),
                        Fill = new SolidColorPaint(CorAzul),
                        Name = "Receita",
                        MaxBarWidth = 50,
                        Padding = 8,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 12,
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top,
                        DataLabelsFormatter = point => FormatarMoeda(point.Model, "C0"),
                        YToolTipLabelFormatter = point => FormatarMoeda(point.Model, "C2")
                    }
                };

                chartReceitaMensal.XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = dadosPorMes.Select(d => d.Label).ToArray(),
                        LabelsPaint = new SolidColorPaint(CorTextoEixo),
                        TextSize = 12,
                        MinStep = 1,
                        ForceStepToMin = true,
                        LabelsRotation = dadosPorMes.Count > 6 ? 45 : 0
                    }
                };

                chartReceitaMensal.YAxes = new Axis[]
                {
                    new Axis
                    {
                        LabelsPaint = new SolidColorPaint(CorTextoEixo),
                        TextSize = 11,
                        Labeler = value => FormatarMoeda(value, "C0"),
                        MinLimit = 0
                    }
                };

                chartReceitaMensal.TooltipBackgroundPaint = new SolidColorPaint(new SKColor(30, 41, 59));
                chartReceitaMensal.TooltipTextPaint = new SolidColorPaint(SKColors.White);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao carregar gráfico de receita mensal");
            }
        }

        private void CarregarGraficoCategoria()
        {
            try
            {
                var dadosPorCategoria = _aulas
                    .GroupBy(a => string.IsNullOrWhiteSpace(a.Categoria) ? "Sem Categoria" : a.Categoria)
                    .Select(g => new
                    {
                        Categoria = g.Key,
                        Valor = (double)g.Sum(a => a.Valor),
                        Count = g.Count()
                    })
                    .OrderByDescending(d => d.Valor)
                    .ToList();

                if (dadosPorCategoria.Count == 0)
                {
                    txtSemDadosCategoria.Visibility = Visibility.Visible;
                    chartCategoria.Visibility = Visibility.Collapsed;
                    return;
                }

                var series = new List<ISeries>();
                for (int i = 0; i < dadosPorCategoria.Count; i++)
                {
                    var d = dadosPorCategoria[i];
                    var cor = CoresCategorias[i % CoresCategorias.Length];
                    series.Add(new PieSeries<double>
                    {
                        Values = new[] { d.Valor },
                        Name = $"{d.Categoria} ({d.Count})",
                        Fill = new SolidColorPaint(cor),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 13,
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point => FormatarMoeda(point.Model, "C2"),
                        ToolTipLabelFormatter = point => $"{d.Categoria}: {FormatarMoeda(point.Model, "C2")}"
                    });
                }

                chartCategoria.Series = series;
                chartCategoria.LegendPosition = LiveChartsCore.Measure.LegendPosition.Right;
                chartCategoria.LegendTextPaint = new SolidColorPaint(SKColors.White);
                chartCategoria.LegendTextSize = 13;
                chartCategoria.TooltipBackgroundPaint = new SolidColorPaint(new SKColor(30, 41, 59));
                chartCategoria.TooltipTextPaint = new SolidColorPaint(SKColors.White);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao carregar gráfico de categorias");
            }
        }

        private void CarregarGraficoStatus()
        {
            try
            {
                var pagos = _aulas.Where(a => a.Status == "Pago").Sum(a => (double)a.Valor);
                var pendentes = _aulas.Where(a => a.Status == "Pendente").Sum(a => (double)a.Valor);
                var qtdPagos = _aulas.Count(a => a.Status == "Pago");
                var qtdPendentes = _aulas.Count(a => a.Status == "Pendente");

                if (pagos + pendentes == 0)
                {
                    txtSemDadosStatus.Visibility = Visibility.Visible;
                    chartStatus.Visibility = Visibility.Collapsed;
                    return;
                }

                var series = new List<ISeries>();

                if (pagos > 0)
                {
                    series.Add(new PieSeries<double>
                    {
                        Values = new[] { pagos },
                        Name = $"Pago ({qtdPagos})",
                        Fill = new SolidColorPaint(CorVerde),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 14,
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point => FormatarMoeda(point.Model, "C2"),
                        ToolTipLabelFormatter = point => $"Pago: {FormatarMoeda(point.Model, "C2")}"
                    });
                }
                
                if (pendentes > 0)
                {
                    series.Add(new PieSeries<double>
                    {
                        Values = new[] { pendentes },
                        Name = $"Pendente ({qtdPendentes})",
                        Fill = new SolidColorPaint(CorLaranja),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 14,
                        DataLabelsPosition = LiveChartsCore.Measure.PolarLabelsPosition.Middle,
                        DataLabelsFormatter = point => FormatarMoeda(point.Model, "C2"),
                        ToolTipLabelFormatter = point => $"Pendente: {FormatarMoeda(point.Model, "C2")}"
                    });
                }

                chartStatus.Series = series;
                chartStatus.LegendPosition = LiveChartsCore.Measure.LegendPosition.Right;
                chartStatus.LegendTextPaint = new SolidColorPaint(SKColors.White);
                chartStatus.LegendTextSize = 13;
                chartStatus.TooltipBackgroundPaint = new SolidColorPaint(new SKColor(30, 41, 59));
                chartStatus.TooltipTextPaint = new SolidColorPaint(SKColors.White);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao carregar gráfico de status");
            }
        }

        private void CarregarGraficoTopClientes()
        {
            try
            {
                var topClientes = _aulas
                    .GroupBy(a => string.IsNullOrWhiteSpace(a.NomeAula) ? "Sem Descrição" : a.NomeAula)
                    .Select(g => new
                    {
                        NomeCompleto = g.Key,
                        Nome = g.Key.Length > 25 ? g.Key.Substring(0, 22) + "..." : g.Key,
                        Valor = (double)g.Sum(a => a.Valor)
                    })
                    .OrderByDescending(d => d.Valor)
                    .Take(10)
                    .Reverse()
                    .ToList();

                if (topClientes.Count == 0)
                {
                    txtSemDadosTop.Visibility = Visibility.Visible;
                    chartTopClientes.Visibility = Visibility.Collapsed;
                    return;
                }

                // Calcular altura dinâmica baseada na quantidade de itens
                var alturaMinima = Math.Max(280, topClientes.Count * 38);
                chartTopClientes.MinHeight = alturaMinima;

                chartTopClientes.Series = new ISeries[]
                {
                    new RowSeries<double>
                    {
                        Values = topClientes.Select(d => d.Valor).ToArray(),
                        Fill = new SolidColorPaint(CorRoxo),
                        Name = "Valor Total",
                        MaxBarWidth = 28,
                        Padding = 6,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 11,
                        DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
                        DataLabelsFormatter = point => FormatarMoeda(point.Model, "C0"),
                        XToolTipLabelFormatter = point => FormatarMoeda(point.Model, "C2")
                    }
                };

                chartTopClientes.YAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = topClientes.Select(d => d.Nome).ToArray(),
                        LabelsPaint = new SolidColorPaint(CorTextoEixo),
                        TextSize = 11,
                        MinStep = 1,
                        ForceStepToMin = true,
                        SeparatorsPaint = new SolidColorPaint(new SKColor(51, 65, 85)) { StrokeThickness = 0.5f }
                    }
                };

                chartTopClientes.XAxes = new Axis[]
                {
                    new Axis
                    {
                        LabelsPaint = new SolidColorPaint(CorTextoEixo),
                        TextSize = 11,
                        Labeler = value => FormatarMoeda(value, "C0"),
                        MinLimit = 0
                    }
                };

                chartTopClientes.TooltipBackgroundPaint = new SolidColorPaint(new SKColor(30, 41, 59));
                chartTopClientes.TooltipTextPaint = new SolidColorPaint(SKColors.White);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao carregar gráfico de top clientes");
            }
        }

        /// <summary>
        /// Formata um valor numérico como moeda brasileira.
        /// </summary>
        private static string FormatarMoeda(double valor, string formato)
        {
            return valor.ToString(formato, _culturePtBr);
        }

        private void Fechar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
