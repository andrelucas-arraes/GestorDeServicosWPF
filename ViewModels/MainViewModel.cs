using System;
using System.Collections.ObjectModel;
using System.Linq;
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

            // Inicialização de dados
            CarregarDadosIniciaisCommand.Execute(null);
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

        [ObservableProperty] private int _totalAulas;

        // Form Rápido
        [ObservableProperty] private DateTime? _novaData = DateTime.Now;
        [ObservableProperty] private string _novoAluno = string.Empty;
        [ObservableProperty] private string _novaDuracao = "1:00";
        [ObservableProperty] private int _novoStatusIndex = 0;
        
        [ObservableProperty] 
        [NotifyPropertyChangedFor(nameof(IsNovaAula))]
        private string _novaCategoriaSelecionada = "Aula";

        public bool IsNovaAula => NovaCategoriaSelecionada == "Aula";

        [ObservableProperty] private string _novoValorStr = "";

        // Listas Auxiliares
        public ObservableCollection<string> Categorias { get; } = new() 
        { 
            "Aula", 
            "Serviço", 
            "Freelance", 
            "Manutenção", 
            "Outro" 
        };

        public ObservableCollection<KeyValuePair<int, string>> Meses { get; } = new()
        {
            new(0, "Todos"), new(1, "Janeiro"), new(2, "Fevereiro"), new(3, "Março"), new(4, "Abril"),
            new(5, "Maio"), new(6, "Junho"), new(7, "Julho"), new(8, "Agosto"),
            new(9, "Setembro"), new(10, "Outubro"), new(11, "Novembro"), new(12, "Dezembro")
        };

        public ObservableCollection<int> Anos { get; } = new();

        // --- Hooks de Propriedades ---
        // private volatile bool _isDataLoading = false; // Removido para evitar bloqueio indevido
        private Guid _currentLoadId;

        async partial void OnMesSelecionadoChanged(int? value) 
        {
            await CarregarDadosAsync();
        }

        async partial void OnAnoSelecionadoChanged(int? value)
        {
            await CarregarDadosAsync();
        }

        async partial void OnTermoBuscaChanged(string value) 
        {
            // Debounce para evitar queries excessivas no banco enquanto digita
            await Task.Delay(300);
            if (value != TermoBusca) return; // Se o valor mudou durante o delay, ignora
            
            await CarregarDadosAsync();
        }

        // --- Comandos ---

        [RelayCommand]
        private async Task CarregarDadosIniciaisAsync()
        {
            _logger.LogInformation("Iniciando carregamento de dados iniciais...");
            
            // Primeiro carrega os anos (isso popula a lista do ComboBox)
            await CarregarAnosAsync();
            
            await CarregarDadosAsync();
            
            _logger.LogInformation("Inicialização de dados concluída.");
        }

        [RelayCommand]
        private async Task AdicionarAulaAsync()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                // Valida data
                if (!NovaData.HasValue)
                {
                    _dialogService.ShowMessage("Selecione uma data.", "Erro");
                    return;
                }

                DateTime data = NovaData.Value;

                // Usa o parser robusto do FormatUtils (Mesma lógica do Diálogo)
                if (!GestaoAulas.Utils.FormatUtils.TryParseDuracao(NovaDuracao, out double duracaoHoras))
                {
                     _dialogService.ShowMessage("Duração inválida. Use o formato H:MM (ex: 1:30) ou decimal (ex: 1.5).", "Erro");
                     return;
                }

                // (Lógica redundante removida)

                var novaAula = Aula.CriarNova();
                novaAula.Data = data;
                novaAula.NomeAula = NovoAluno;
                novaAula.Categoria = NovaCategoriaSelecionada;
                novaAula.Status = NovoStatusIndex == 1 ? "Pago" : "Pendente";

                if (IsNovaAula)
                {
                    novaAula.Duracao = duracaoHoras;
                    novaAula.RecalcularValor();
                }
                else
                {
                    // Se nao for aula, a duração é 0
                    novaAula.Duracao = 0; 
                    
                    if (GestaoAulas.Utils.FormatUtils.TryParseDecimal(NovoValorStr, out decimal valorManual))
                    {
                        novaAula.Valor = valorManual;
                    }
                    else
                    {
                        // Fallback: Tenta limpar mascaras básicas se o utilitário falhar (improvável)
                        string clean = NovoValorStr.Replace("R$", "").Trim();
                        if (decimal.TryParse(clean, out decimal v)) novaAula.Valor = v;
                    }
                }

                _logger.LogDebug("Iniciando validação e inserção de nova aula: {Aluno}", NovoAluno);

                // Validação via FluentValidation
                var result = await _validator.ValidateAsync(novaAula);
                if (!result.IsValid)
                {
                    _logger.LogWarning("Validação falhou para nova aula: {Error}", result.Errors[0].ErrorMessage);
                    _dialogService.ShowMessage(result.Errors[0].ErrorMessage, "Validação");
                    return;
                }

                // Inserir
                await _repository.InserirAsync(novaAula);
                
                // Salva o ano e mês da aula recém adicionada para ajustar o filtro
                int anoAula = novaAula.Data.Year;
                int mesAula = novaAula.Data.Month;

                // Limpar Form
                NovaData = DateTime.Now;
                NovoAluno = string.Empty;
                NovaDuracao = "1:00";
                NovoStatusIndex = 0;
                // Mantém a categoria selecionada para facilitar entradas sequenciais
                NovoValorStr = "";

                string msgSucesso = $"{novaAula.Categoria} registrado(a)!";
                if (novaAula.Categoria == "Aula") msgSucesso = "Aula registrada!";
                
                _dialogService.ShowMessage(msgSucesso, "Sucesso");

                // Atualiza filtros para o período da aula adicionada (para que o usuário veja que salvou)
                await CarregarAnosAsync(); 
                AnoSelecionado = anoAula;
                MesSelecionado = mesAula;
                
                await CarregarDadosAsync();
                
                sw.Stop();
                _logger.LogInformation("Nova aula registrada com sucesso em {Elapsed}ms. Aluno: {Aluno}", sw.ElapsedMilliseconds, novaAula.NomeAula);
            }
            catch (Exception ex)
            {
                sw.Stop();
                _logger.LogError(ex, "Erro ao adicionar aula após {Elapsed}ms. Aluno={NovoAluno}", sw.ElapsedMilliseconds, NovoAluno);
                _dialogService.ShowMessage($"Erro ao salvar aula:\n\n{ex.Message}\n\nDetalhes: {ex.GetType().Name}", "Erro");
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditAula))]
        private async Task EditarAulaAsync()
        {
            if (AulaSelecionada == null) return;
            
            try
            {
                _logger.LogDebug("Abrindo edição de aula. ID={Id}, Nome={Nome}", AulaSelecionada.Id, AulaSelecionada.NomeAula);
                
                // Clone para não afetar a lista antes de salvar
                var clone = AulaSelecionada.Clone();
                
                if (_dialogService.ShowAulaDialog(clone))
                {
                    _logger.LogInformation("Salvando alterações da aula ID={Id}", clone.Id);
                    await _repository.AtualizarAsync(clone);
                    await CarregarAnosAsync(); // Pode ter mudado o ano da aula
                    await CarregarDadosAsync();
                    _logger.LogInformation("Aula editada com sucesso. ID={Id}, Nome={Nome}", clone.Id, clone.NomeAula);
                }
                else
                {
                    _logger.LogDebug("Edição de aula cancelada pelo usuário. ID={Id}", AulaSelecionada.Id);
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
                _logger.LogDebug("Solicitando confirmação para excluir aula. ID={Id}, Nome={Nome}", AulaSelecionada.Id, AulaSelecionada.NomeAula);
                
                if (_dialogService.Confirm($"Excluir aula de {AulaSelecionada.NomeAula}?", "Confirmar"))
                {
                    var idExcluir = AulaSelecionada.Id;
                    var nomeExcluir = AulaSelecionada.NomeAula;
                    
                    await _repository.ExcluirAsync(idExcluir);
                    await CarregarAnosAsync(); // Pode ter removido o último registro de um ano
                    await CarregarDadosAsync();
                    
                    _logger.LogInformation("Aula excluída com sucesso. ID={Id}, Nome={Nome}", idExcluir, nomeExcluir);
                }
                else
                {
                    _logger.LogDebug("Exclusão cancelada pelo usuário. ID={Id}", AulaSelecionada.Id);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir aula ID={Id}", AulaSelecionada?.Id);
                _dialogService.ShowMessage($"Erro ao excluir aula:\n\n{ex.Message}", "Erro");
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditAula))]
        private async Task MarcarComoPagoAsync() => await AlterarStatusAsync("Pago");

        [RelayCommand(CanExecute = nameof(CanEditAula))]
        private async Task MarcarComoPendenteAsync() => await AlterarStatusAsync("Pendente");

        private async Task AlterarStatusAsync(string status)
        {
            if (AulaSelecionada == null) return;
            
            // Salva informações antes de recarregar (pois AulaSelecionada pode ficar null)
            var aulaId = AulaSelecionada.Id;
            var statusAnterior = AulaSelecionada.Status;
            
            try
            {
                _logger.LogDebug("Alterando status da aula. ID={Id}, StatusAnterior={StatusAnterior}, NovoStatus={NovoStatus}", 
                    aulaId, statusAnterior, status);
                
                AulaSelecionada.Status = status;
                await _repository.AtualizarAsync(AulaSelecionada);
                await CarregarDadosAsync();
                
                _logger.LogInformation("Status da aula alterado. ID={Id}, NovoStatus={Status}", aulaId, status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao alterar status da aula ID={Id} para {Status}", aulaId, status);
                _dialogService.ShowMessage($"Erro ao alterar status:\n\n{ex.Message}", "Erro");
            }
        }

        [RelayCommand]
        private void ExportarDados()
        {
            try
            {
                int? mes = MesSelecionado > 0 ? MesSelecionado : null;
                int? ano = AnoSelecionado > 0 ? AnoSelecionado : null;
                
                _logger.LogInformation("Abrindo diálogo de exportação. Total de aulas={Count}, Mes={Mes}, Ano={Ano}", 
                    _cacheAulasBanco.Count, mes, ano);
                    
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
                _logger.LogDebug("Abrindo diálogo de configurações");
                _dialogService.ShowConfiguracoesDialog();
                _logger.LogDebug("Diálogo de configurações fechado");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao abrir configurações");
                _dialogService.ShowMessage($"Erro ao abrir configurações:\n\n{ex.Message}", "Erro");
            }
        }

        // --- Métodos Privados ---

        private async Task CarregarDadosAsync()
        {
            // Gera um ID único para esta solicitação
            var loadId = Guid.NewGuid();
            _currentLoadId = loadId;
            
            var sw = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                int? mes = MesSelecionado > 0 ? MesSelecionado : null;
                int? ano = AnoSelecionado > 0 ? AnoSelecionado : null;

                _logger.LogDebug("Iniciando carregamento de dados. ReqID={ReqId}, Filtros: Mes={Mes}, Ano={Ano}, Busca={Busca}", 
                    loadId, mes, ano, TermoBusca);
                
                // Busca no banco (assíncrono)
                var dadosBanco = await _repository.ObterTodasAsync(mes, ano, TermoBusca);
                
                // Se o ID da solicitação mudou durante o await, significa que uma nova busca foi iniciada.
                // Descartamos este resultado para evitar condição de corrida (dados antigos sobrescrevendo novos).
                if (_currentLoadId != loadId)
                {
                     _logger.LogDebug("Resultado de carregamento descartado por obsolescência. ReqID={ReqId}", loadId);
                     return;
                }

                // Atualiza coleção
                _cacheAulasBanco = dadosBanco;
                Aulas = new ObservableCollection<Aula>(_cacheAulasBanco);
                
                CalcularEstatisticas();
                
                sw.Stop();
                _logger.LogInformation("Dados carregados com sucesso em {Elapsed}ms. ReqID={ReqId}, Total exibido: {Count}", 
                    sw.ElapsedMilliseconds, loadId, Aulas.Count);
            }
            catch (Exception ex)
            {
                if (sw.IsRunning) sw.Stop();
                _logger.LogError(ex, "Falha ao carregar dados. Tempo decorrido: {Elapsed}ms", sw.ElapsedMilliseconds);
                _dialogService.ShowMessage($"Erro ao carregar dados:\n\n{ex.Message}", "Erro");
            }
        }

        // Método FiltrarLocalmente removido pois o filtro agora é no SQL (Performance Tuning)


        private void CalcularEstatisticas()
        {
            TotalHoras = _cacheAulasBanco.Where(a => a.Categoria == "Aula").Sum(a => a.Duracao);
            TotalRecebido = _cacheAulasBanco.Where(a => a.Status == "Pago").Sum(a => a.Valor);
            TotalPendente = _cacheAulasBanco.Where(a => a.Status == "Pendente").Sum(a => a.Valor);
            TotalAulas = _cacheAulasBanco.Count;
        }

        private async Task CarregarAnosAsync()
        {
            try
            {
                _logger.LogDebug("Atualizando lista de anos disponíveis...");
                var anosDb = await _repository.ObterAnosDisponiveisAsync();
                
                if (!anosDb.Contains(DateTime.Now.Year)) 
                    anosDb.Add(DateTime.Now.Year);
                
                // Preserva o ano atual selecionado
                var anoAtual = AnoSelecionado;

                Anos.Clear();
                foreach (var ano in anosDb.OrderByDescending(x => x)) 
                    Anos.Add(ano);
                
                // Tenta restaurar o ano que estava selecionado ou seleciona o primeiro
                if (anoAtual.HasValue && Anos.Contains(anoAtual.Value))
                    AnoSelecionado = anoAtual;
                else if (Anos.Count > 0)
                    AnoSelecionado = Anos[0];
                    
                _logger.LogDebug("Filtro de anos atualizado. Total: {Count}", Anos.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar anos disponíveis.");
            }
        }

        // Propriedades Formatadas para Dashboard (Helpers)
        public string TotalHorasFormatado 
        {
            get 
            {
                int h = (int)TotalHoras;
                int m = (int)((TotalHoras - h) * 60);
                return $"{h}h{m:D2}";
            }
        }
        public string TotalRecebidoFormatado => TotalRecebido.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
        public string TotalPendenteFormatado => TotalPendente.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));
    }
}
