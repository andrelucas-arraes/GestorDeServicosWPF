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

        public AnalisesDialog(List<Aula> aulas)
        {
            InitializeComponent();
            _aulas = aulas ?? new List<Aula>();

            Loaded += (s, e) =>
            {
                CarregarResumo();
                CarregarGraficoReceitaMensal();
                CarregarGraficoCategoria();
                CarregarGraficoStatus();
                CarregarGraficoTopClientes();
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

                if (dadosPorMes.Count == 0) return;

                chartReceitaMensal.Series = new ISeries[]
                {
                    new ColumnSeries<double>
                    {
                        Values = dadosPorMes.Select(d => d.Valor).ToArray(),
                        Fill = new SolidColorPaint(new SKColor(59, 130, 246)),
                        Name = "Receita",
                        MaxBarWidth = 40,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 11,
                        DataLabelsFormatter = point => point.Model.ToString("C0", _culturePtBr),
                        YToolTipLabelFormatter = point => point.Model.ToString("C2", _culturePtBr)
                    }
                };

                chartReceitaMensal.XAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = dadosPorMes.Select(d => d.Label).ToArray(),
                        LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                        TextSize = 11,
                        MinStep = 1
                    }
                };

                chartReceitaMensal.YAxes = new Axis[]
                {
                    new Axis
                    {
                        LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                        TextSize = 11,
                        Labeler = (value) => value.ToString("C0", _culturePtBr),
                        MinLimit = 0
                    }
                };
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
                    .GroupBy(a => a.Categoria)
                    .Select(g => new
                    {
                        Categoria = g.Key,
                        Valor = (double)g.Sum(a => a.Valor),
                        Count = g.Count()
                    })
                    .OrderByDescending(d => d.Valor)
                    .ToList();

                if (dadosPorCategoria.Count == 0) return;

                var cores = new SKColor[]
                {
                    new SKColor(59, 130, 246),   // Azul
                    new SKColor(16, 185, 129),   // Verde
                    new SKColor(249, 115, 22),   // Laranja
                    new SKColor(167, 139, 250),  // Roxo
                    new SKColor(236, 72, 153),   // Rosa
                    new SKColor(234, 179, 8),    // Amarelo
                    new SKColor(20, 184, 166),   // Teal
                    new SKColor(239, 68, 68),    // Vermelho
                };

                var series = new List<ISeries>();
                for (int i = 0; i < dadosPorCategoria.Count; i++)
                {
                    var d = dadosPorCategoria[i];
                    var cor = cores[i % cores.Length];
                    series.Add(new PieSeries<double>
                    {
                        Values = new[] { d.Valor },
                        Name = $"{d.Categoria} ({d.Count})",
                        Fill = new SolidColorPaint(cor),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 12,
                        DataLabelsFormatter = point => point.Model.ToString("C2", _culturePtBr),
                        ToolTipLabelFormatter = point => point.Model.ToString("C2", _culturePtBr)
                    });
                }

                chartCategoria.Series = series;
                chartCategoria.LegendPosition = LiveChartsCore.Measure.LegendPosition.Right;
                chartCategoria.LegendTextPaint = new SolidColorPaint(SKColors.White);
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

                if (pagos + pendentes == 0) return;

                var series = new List<ISeries>();

                if (pagos > 0)
                {
                    series.Add(new PieSeries<double>
                    {
                        Values = new[] { pagos },
                        Name = $"Pago ({qtdPagos})",
                        Fill = new SolidColorPaint(new SKColor(34, 197, 94)),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 13,
                        DataLabelsFormatter = point => point.Model.ToString("C2", _culturePtBr),
                        ToolTipLabelFormatter = point => point.Model.ToString("C2", _culturePtBr)
                    });
                }
                
                if (pendentes > 0)
                {
                    series.Add(new PieSeries<double>
                    {
                        Values = new[] { pendentes },
                        Name = $"Pendente ({qtdPendentes})",
                        Fill = new SolidColorPaint(new SKColor(249, 115, 22)),
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 13,
                        DataLabelsFormatter = point => point.Model.ToString("C2", _culturePtBr),
                        ToolTipLabelFormatter = point => point.Model.ToString("C2", _culturePtBr)
                    });
                }

                chartStatus.Series = series;
                chartStatus.LegendPosition = LiveChartsCore.Measure.LegendPosition.Right;
                chartStatus.LegendTextPaint = new SolidColorPaint(SKColors.White);
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
                    .GroupBy(a => a.NomeAula)
                    .Select(g => new
                    {
                        Nome = string.IsNullOrWhiteSpace(g.Key) ? "Sem Descrição" : (g.Key.Length > 15 ? g.Key.Substring(0, 15) + "..." : g.Key),
                        Valor = (double)g.Sum(a => a.Valor)
                    })
                    .OrderByDescending(d => d.Valor)
                    .Take(10)
                    .Reverse()
                    .ToList();

                if (topClientes.Count == 0) return;

                chartTopClientes.Series = new ISeries[]
                {
                    new RowSeries<double>
                    {
                        Values = topClientes.Select(d => d.Valor).ToArray(),
                        Fill = new SolidColorPaint(new SKColor(167, 139, 250)),
                        Name = "Valor Total",
                        MaxBarWidth = 30,
                        DataLabelsPaint = new SolidColorPaint(SKColors.White),
                        DataLabelsSize = 11,
                        DataLabelsFormatter = point => point.Model.ToString("C0", _culturePtBr),
                        XToolTipLabelFormatter = point => point.Model.ToString("C2", _culturePtBr)
                    }
                };

                chartTopClientes.YAxes = new Axis[]
                {
                    new Axis
                    {
                        Labels = topClientes.Select(d => d.Nome).ToArray(),
                        LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                        TextSize = 11,
                        MinStep = 1
                    }
                };

                chartTopClientes.XAxes = new Axis[]
                {
                    new Axis
                    {
                        LabelsPaint = new SolidColorPaint(new SKColor(148, 163, 184)),
                        TextSize = 11,
                        Labeler = (value) => value.ToString("C0", _culturePtBr),
                        MinLimit = 0
                    }
                };
            }
            catch (Exception ex)
            {
                Serilog.Log.Error(ex, "Erro ao carregar gráfico de top clientes");
            }
        }

        private void Fechar_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
