using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GestaoAulas.Models;
using GestaoAulas.Services;
using GestaoAulas.Repositories;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace GestaoAulas.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly IAulaRepository _repository;
        private readonly IDialogService _dialogService;
        private readonly IValidator<Aula> _validator;
        private readonly ILogger<MainViewModel> _logger;

        private List<Aula> _cacheAulasBanco = new();

        public MainViewModel(
            IAulaRepository repository, 
            IDialogService dialogService, 
            IValidator<Aula> validator,
            ILogger<MainViewModel> logger)
        {
            _repository = repository;
            _dialogService = dialogService;
            _validator = validator;
            _logger = logger;
        }

        /// <summary>
        /// Inicializa o ViewModel carregando dados de forma assíncrona.
        /// </summary>
        public async Task InitializeAsync()
        {
            await CarregarDadosIniciaisAsync();
        }

        // --- Propriedades Observáveis ---

        [ObservableProperty]
        private ObservableCollection<Aula> _aulas = new();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(EditarAulaCommand))]
        [NotifyCanExecuteChangedFor(nameof(ExcluirAulaCommand))]
        [NotifyCanExecuteChangedFor(nameof(MarcarComoPagoCommand))]
        [NotifyCanExecuteChangedFor(nameof(MarcarComoPendenteCommand))]
        private Aula? _aulaSelecionada;

        [ObservableProperty]
        private int? _mesSelecionado = DateTime.Now.Month;

        [ObservableProperty]
        private int? _anoSelecionado = DateTime.Now.Year;

        [ObservableProperty]
        private string _termoBusca = string.Empty;

        [ObservableProperty]
        private string _filtroStatusSelecionado = "Todos";

        // Dashboard
        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(TotalHorasFormatado))]
        private double _totalHoras;

        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(TotalRecebidoFormatado))]
        private decimal _totalRecebido;

        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(TotalPendenteFormatado))]
        private decimal _totalPendente;

        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(TotalPendenteAnoFormatado))]
        private decimal _totalPendenteAno;

        [ObservableProperty] private int _totalAulas;

        // Form Rápido
        [ObservableProperty] private DateTime? _novaData = DateTime.Now;
        [ObservableProperty] private string _novoAluno = string.Empty;
        [ObservableProperty] private string _novoValorStr = "";
        [ObservableProperty] private int _novoStatusIndex = 0;
        
        [ObservableProperty] 
        private string _novaCategoriaSelecionada = "Aula";

        public bool IsNovaAula => NovaCategoriaSelecionada == "Aula";

        [ObservableProperty] private string _novaTag = string.Empty;

        // Listas Auxiliares
        public ObservableCollection<string> Categorias { get; } = new();

        public ObservableCollection<string> FiltroStatusOptions { get; } = new()
        {
            "Todos", "Pendente", "Pago"
        };

        public ObservableCollection<KeyValuePair<int, string>> Meses { get; } = new()
        {
            new(0, "Todos"), new(1, "Janeiro"), new(2, "Fevereiro"), new(3, "Março"), new(4, "Abril"),
            new(5, "Maio"), new(6, "Junho"), new(7, "Julho"), new(8, "Agosto"),
            new(9, "Setembro"), new(10, "Outubro"), new(11, "Novembro"), new(12, "Dezembro")
        };

        public ObservableCollection<int> Anos { get; } = new();

        // --- Hooks de Propriedades ---
        private Guid _currentLoadId;
        private bool _suppressAutoLoad;
        private CancellationTokenSource? _debounceCts;

        partial void OnNovaCategoriaSelecionadaChanged(string value)
        {
            OnPropertyChanged(nameof(IsNovaAula));
        }

        async partial void OnMesSelecionadoChanged(int? value) 
        {
            if (_suppressAutoLoad) return;
            try { await CarregarDadosAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Erro no hook OnMesSelecionadoChanged"); }
        }

        async partial void OnAnoSelecionadoChanged(int? value)
        {
            if (_suppressAutoLoad) return;
            try { await CarregarDadosAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Erro no hook OnAnoSelecionadoChanged"); }
        }

        async partial void OnFiltroStatusSelecionadoChanged(string value)
        {
            if (_suppressAutoLoad) return;
            try { await CarregarDadosAsync(); }
            catch (Exception ex) { _logger.LogError(ex, "Erro no hook OnFiltroStatusSelecionadoChanged"); }
        }

        async partial void OnTermoBuscaChanged(string value) 
        {
            try
            {
                _debounceCts?.Cancel();
                _debounceCts?.Dispose();
                var cts = new CancellationTokenSource();
                _debounceCts = cts;
                
                await Task.Delay(300, cts.Token);
                if (cts.Token.IsCancellationRequested || value != TermoBusca) return;
                
                await CarregarDadosAsync();
            }
            catch (OperationCanceledException) { }
            catch (Exception ex) { _logger.LogError(ex, "Erro no hook OnTermoBuscaChanged"); }
        }

        // --- Comandos ---

        [RelayCommand]
        private async Task CarregarDadosIniciaisAsync()
        {
            _logger.LogInformation("Iniciando carregamento de dados iniciais...");
            
            _suppressAutoLoad = true;
            try
            {
                await CarregarCategoriasAsync();
                await CarregarAnosAsync();
            }
            finally
            {
                _suppressAutoLoad = false;
            }
            
            await CarregarDadosAsync();
            
            _logger.LogInformation("Inicialização de dados concluída.");
        }

        private async Task CarregarCategoriasAsync()
        {
            try
            {
                var categoriasDb = await _repository.ObterCategoriasAsync();
                Categorias.Clear();
                foreach (var cat in categoriasDb)
                    Categorias.Add(cat);

                // Garante que a categoria selecionada padrão exista
                if (!Categorias.Contains(NovaCategoriaSelecionada) && Categorias.Count > 0)
                    NovaCategoriaSelecionada = Categorias[0];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar categorias");
                if (Categorias.Count == 0)
                {
                    Categorias.Add("Aula");
                    Categorias.Add("Serviço");
                    Categorias.Add("Freelance");
                    Categorias.Add("Manutenção");
                    Categorias.Add("Outro");
                }
            }
        }

        [RelayCommand]
        private async Task AdicionarCategoriaAsync(string novaCategoria)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(novaCategoria))
                {
                    _dialogService.ShowMessage("O nome da categoria não pode ser vazio.", "Validação");
                    return;
                }

                var nome = novaCategoria.Trim();
                if (Categorias.Contains(nome))
                {
                    _dialogService.ShowMessage("Essa categoria já existe.", "Validação");
                    return;
                }

                await _repository.AdicionarCategoriaAsync(nome);
                Categorias.Add(nome);
                _logger.LogInformation("Nova categoria adicionada: {Categoria}", nome);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar categoria");
                _dialogService.ShowMessage($"Erro ao adicionar categoria: {ex.Message}", "Erro");
            }
        }

        [RelayCommand]
        private async Task AdicionarAulaAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                if (!NovaData.HasValue)
                {
                    _dialogService.ShowMessage("Selecione uma data.", "Erro");
                    return;
                }

                DateTime data = NovaData.Value;

                // Valida valor para todas as categorias
                if (!GestaoAulas.Utils.FormatUtils.TryParseDecimal(NovoValorStr, out decimal valorFinal))
                {
                    _dialogService.ShowMessage("Valor inválido. Informe um valor numérico válido (ex: 50,00).", "Erro");
                    return;
                }

                var novaAula = Aula.CriarNova();
                novaAula.Data = data;
                novaAula.NomeAula = NovoAluno;
                novaAula.Categoria = NovaCategoriaSelecionada;
                novaAula.Tag = NovaTag?.Trim() ?? "";
                novaAula.Status = NovoStatusIndex == 1 ? "Pago" : "Pendente";
                novaAula.Valor = valorFinal;
                novaAula.Duracao = 0;

                _logger.LogDebug("Validando nova aula: {Aluno}", NovoAluno);

                var result = await _validator.ValidateAsync(novaAula);
                if (!result.IsValid)
                {
                    _logger.LogWarning("Validação falhou: {Error}", result.Errors[0].ErrorMessage);
                    _dialogService.ShowMessage(result.Errors[0].ErrorMessage, "Validação");
                    return;
                }

                await _repository.InserirAsync(novaAula);
                
                int anoAula = novaAula.Data.Year;
                int mesAula = novaAula.Data.Month;

                // Limpar Form
                NovaData = DateTime.Now;
                NovoAluno = string.Empty;
                NovoStatusIndex = 0;
                NovoValorStr = "";
                NovaTag = "";

                string msgSucesso = $"{novaAula.Categoria} registrado(a)!";
                _dialogService.ShowMessage(msgSucesso, "Sucesso");

                _suppressAutoLoad = true;
                try
                {
                    await CarregarAnosAsync(); 
                    AnoSelecionado = anoAula;
                    MesSelecionado = mesAula;
                }
                finally
                {
                    _suppressAutoLoad = false;
                }
                
                await CarregarDadosAsync();
                
                sw.Stop();
                _logger.LogInformation("Registro salvo em {Elapsed}ms. Nome: {Aluno}", sw.ElapsedMilliseconds, novaAula.NomeAula);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Erro ao adicionar aula após {Elapsed}ms", sw.ElapsedMilliseconds);
                _dialogService.ShowMessage("Erro ao salvar o registro. Verifique os dados e tente novamente.", "Erro");
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditAula))]
        private async Task EditarAulaAsync()
        {
            if (AulaSelecionada == null) return;
            
            try
            {
                var clone = AulaSelecionada.Clone();
                
                if (_dialogService.ShowAulaDialog(clone))
                {
                    await _repository.AtualizarAsync(clone);
                    await CarregarAnosAsync();
                    await CarregarDadosAsync();
                    _logger.LogInformation("Aula editada. ID={Id}", clone.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar aula ID={Id}", AulaSelecionada?.Id);
                _dialogService.ShowMessage($"Erro ao editar aula:\n\n{ex.Message}", "Erro");
            }
        }
        private bool CanEditAula() => AulaSelecionada != null;

        [RelayCommand(CanExecute = nameof(CanEditAula))]
        private async Task ExcluirAulaAsync()
        {
            if (AulaSelecionada == null) return;

            try
            {
                if (_dialogService.Confirm($"Excluir registro de {AulaSelecionada.NomeAula}?", "Confirmar"))
                {
                    var idExcluir = AulaSelecionada.Id;
                    
                    await _repository.ExcluirAsync(idExcluir);
                    await CarregarAnosAsync();
                    await CarregarDadosAsync();
                    
                    _logger.LogInformation("Registro excluído. ID={Id}", idExcluir);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir aula ID={Id}", AulaSelecionada?.Id);
                _dialogService.ShowMessage($"Erro ao excluir:\n\n{ex.Message}", "Erro");
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditAula))]
        private async Task MarcarComoPagoAsync() => await AlterarStatusAsync("Pago");

        [RelayCommand(CanExecute = nameof(CanEditAula))]
        private async Task MarcarComoPendenteAsync() => await AlterarStatusAsync("Pendente");

        private async Task AlterarStatusAsync(string status)
        {
            if (AulaSelecionada == null) return;
            
            var aula = AulaSelecionada;
            var aulaId = aula.Id;
            
            try
            {
                var clone = aula.Clone();
                clone.Status = status;
                clone.DataAtualizacao = DateTime.Now;
                
                await _repository.AtualizarAsync(clone);
                await CarregarDadosAsync();
                
                _logger.LogInformation("Status alterado. ID={Id}, NovoStatus={Status}", aulaId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar status da aula ID={Id}", aulaId);
                _dialogService.ShowMessage("Erro ao alterar status. Tente novamente.", "Erro");
            }
        }

        [RelayCommand]
        private void ExportarDados()
        {
            try
            {
                int? mes = MesSelecionado > 0 ? MesSelecionado : null;
                int? ano = AnoSelecionado > 0 ? AnoSelecionado : null;
                    
                _dialogService.ShowExportDialog(_cacheAulasBanco, mes, ano);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao abrir diálogo de exportação");
                _dialogService.ShowMessage($"Erro ao exportar:\n\n{ex.Message}", "Erro");
            }
        }

        [RelayCommand]
        private void AbrirConfiguracoes()
        {
            try
            {
                _dialogService.ShowConfiguracoesDialog();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao abrir configurações");
                _dialogService.ShowMessage($"Erro ao abrir configurações:\n\n{ex.Message}", "Erro");
            }
        }

        [RelayCommand]
        private void AbrirAnalises()
        {
            try
            {
                _dialogService.ShowAnalisesDialog(_cacheAulasBanco);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao abrir análises");
                _dialogService.ShowMessage($"Erro ao abrir análises:\n\n{ex.Message}", "Erro");
            }
        }

        // --- Métodos Privados ---

        private async Task CarregarDadosAsync()
        {
            var loadId = Guid.NewGuid();
            _currentLoadId = loadId;
            
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                int? mes = MesSelecionado > 0 ? MesSelecionado : null;
                int? ano = AnoSelecionado > 0 ? AnoSelecionado : null;
                string? filtroStatus = FiltroStatusSelecionado != "Todos" ? FiltroStatusSelecionado : null;
                
                var dadosBanco = await _repository.ObterTodasAsync(mes, ano, TermoBusca, filtroStatus);
                
                if (_currentLoadId != loadId)
                {
                     return;
                }

                _cacheAulasBanco = dadosBanco;
                Aulas = new ObservableCollection<Aula>(_cacheAulasBanco);
                
                await CalcularEstatisticasAsync();
                
                sw.Stop();
                _logger.LogInformation("Dados carregados em {Elapsed}ms. Total: {Count}", sw.ElapsedMilliseconds, Aulas.Count);
            }
            catch (Exception ex)
            {
                if (sw.IsRunning) sw.Stop();
                _logger.LogError(ex, "Falha ao carregar dados. Tempo: {Elapsed}ms", sw.ElapsedMilliseconds);
                _dialogService.ShowMessage($"Erro ao carregar dados:\n\n{ex.Message}", "Erro");
            }
        }

        private async Task CalcularEstatisticasAsync()
        {
            TotalHoras = _cacheAulasBanco.Where(a => a.Categoria == "Aula").Sum(a => a.Duracao);
            TotalRecebido = _cacheAulasBanco.Where(a => a.Status == "Pago").Sum(a => a.Valor);
            TotalPendente = _cacheAulasBanco.Where(a => a.Status == "Pendente").Sum(a => a.Valor);
            TotalAulas = _cacheAulasBanco.Count;

            try
            {
                int? ano = AnoSelecionado > 0 ? AnoSelecionado : null;
                TotalPendenteAno = await _repository.ObterTotalPendenteAnoAsync(ano);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular total pendente do ano");
                TotalPendenteAno = 0;
            }
        }

        private async Task CarregarAnosAsync()
        {
            try
            {
                var anosDb = await _repository.ObterAnosDisponiveisAsync();
                
                if (!anosDb.Contains(DateTime.Now.Year)) 
                    anosDb.Add(DateTime.Now.Year);
                
                var anoAtual = AnoSelecionado;

                Anos.Clear();
                foreach (var ano in anosDb.OrderByDescending(x => x)) 
                    Anos.Add(ano);
                
                if (anoAtual.HasValue && Anos.Contains(anoAtual.Value))
                    AnoSelecionado = anoAtual;
                else if (Anos.Count > 0)
                    AnoSelecionado = Anos[0];
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar anos disponíveis.");
            }
        }

        // Propriedades Formatadas para Dashboard
        public string TotalHorasFormatado 
        {
            get 
            {
                int h = (int)TotalHoras;
                int m = (int)((TotalHoras - h) * 60);
                return $"{h}h{m:D2}";
            }
        }
        private static readonly System.Globalization.CultureInfo _culturePtBr = new("pt-BR");
        public string TotalRecebidoFormatado => TotalRecebido.ToString("C2", _culturePtBr);
        public string TotalPendenteFormatado => TotalPendente.ToString("C2", _culturePtBr);
        public string TotalPendenteAnoFormatado => TotalPendenteAno.ToString("C2", _culturePtBr);
    }
}
