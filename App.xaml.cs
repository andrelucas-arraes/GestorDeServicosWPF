using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using GestaoAulas.Repositories;
using GestaoAulas.Services;
using GestaoAulas.ViewModels;
using GestaoAulas.Validators;
using FluentValidation;

namespace GestaoAulas
{
    /// <summary>
    /// Classe de entrada da aplicação.
    /// Configura DI, Logging e Inicialização.
    /// </summary>
    public partial class App : Application
    {
        public static IHost? AppHost { get; private set; }
        private static System.Threading.Mutex? _mutex;

        public App()
        {
            // Configura cultura para pt-BR
            System.Globalization.CultureInfo.DefaultThreadCurrentCulture = new System.Globalization.CultureInfo("pt-BR");
            System.Globalization.CultureInfo.DefaultThreadCurrentUICulture = new System.Globalization.CultureInfo("pt-BR");
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("pt-BR");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("pt-BR");

            // Força os componentes WPF a usarem pt-BR
            FrameworkElement.LanguageProperty.OverrideMetadata(
                typeof(FrameworkElement),
                new FrameworkPropertyMetadata(System.Windows.Markup.XmlLanguage.GetLanguage("pt-BR")));

            // Verifica instância única
            _mutex = new System.Threading.Mutex(true, "GestaoAulasWPF-Unique-Mutex", out bool createdNew);
            if (!createdNew)
            {
                MessageBox.Show("O aplicativo já está em execução.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                Environment.Exit(0);
                return;
            }

            // Configuração do Serilog (Local, conforme solicitado)
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .Enrich.FromLogContext()
                .WriteTo.File("logs/log-.csv", 
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff};{Level:u3};{Message:lj};{Exception}{NewLine}",
                    rollingInterval: RollingInterval.Day,
                    shared: true,
                    flushToDiskInterval: TimeSpan.FromSeconds(1),
                    retainedFileCountLimit: 7,
                    fileSizeLimitBytes: 10485760,
                    rollOnFileSizeLimit: true)
                .CreateLogger();

            // Configuração do Host Genérico
            AppHost = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddSingleton<IAulaRepository, AulaRepository>();
                    services.AddSingleton<IDialogService, DialogService>();
                    services.AddSingleton<BackupManager>();
                    services.AddTransient<IValidator<Models.Aula>, AulaValidator>();
                    services.AddTransient<MainViewModel>();
                    services.AddTransient<MainWindow>();
                    Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
                })
                .UseSerilog()
                .Build();

            // Tratamento de exceções (mantido igual)
            DispatcherUnhandledException += (s, e) => 
            {
                Log.Error(e.Exception, "Erro não tratado na thread de UI (Dispatcher)");
                e.Handled = true;
                
                var owner = Current.MainWindow;
                if (owner != null && owner.IsVisible)
                {
                    MessageBox.Show(owner, $"Erro inesperado: {e.Exception.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    MessageBox.Show($"Erro inesperado: {e.Exception.Message}", "Erro Crítico", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };

            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                {
                    Log.Fatal(ex, "Erro fatal não tratado (AppDomain)");
                    MessageBox.Show($"Erro fatal do sistema: {ex.Message}\n\nO aplicativo será encerrado.", "Erro Fatal", MessageBoxButton.OK, MessageBoxImage.Stop);
                }
            };
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            try
            {
                if (AppHost == null) return;
                
                Log.Information(">>> Aplicação Iniciando <<<");
                await AppHost.StartAsync().ConfigureAwait(true);

                // --- MIGRAÇÃO E CONFIGURAÇÃO DE AMBIENTE ---
                // Define caminhos seguros (AppData) para o BANCO DE DADOS (Mantido por segurança)
#if DEBUG
                // Em DEBUG: Usa uma pasta local "DevData" para não misturar com o banco de produção do usuário
                string appDataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DevData");
#else
                // Em RELEASE: Usa o AppData do usuário (Caminho Padrão)
                string oldAppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestaoAulas");
                string appDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestorDeServicos");
#endif
                Directory.CreateDirectory(appDataFolder);

                string dbNome = "aulas.db";
                string dbOrigem = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, dbNome);
                string dbDestino = Path.Combine(appDataFolder, dbNome);

                // MIGRACAO 1: De "GestaoAulas" (antigo) para "GestorDeServicos" (novo)
                // Nota: Em DEBUG, não tenta migrar do AppData de produção para evitar misturar dados.
                if (!File.Exists(dbDestino))
                {
#if !DEBUG
                    // Tenta pegar do AppData antigo primeiro (somente em RELEASE)
                    string dbAntigo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestaoAulas", dbNome);
                    if (File.Exists(dbAntigo))
                    {
                        try 
                        {
                            Log.Information("Migrando banco de 'GestaoAulas' para 'GestorDeServicos'");
                            File.Copy(dbAntigo, dbDestino); 
                        } 
                        catch (Exception ex) 
                        {
                            Log.Error(ex, "Erro ao migrar de pasta antiga");
                        }
                    }
                    // Se não tiver no antigo, tenta pegar do diretório local (legado v1)
                    else if (File.Exists(dbOrigem))
#else
                    // Em DEBUG: apenas tenta copiar do diretório local (legado v1)
                    if (File.Exists(dbOrigem))
#endif
                    {
                        Log.Information("Detectado banco de dados local legado. Migrando para AppData: {Destino}", dbDestino);
                        try 
                        {
                            File.Copy(dbOrigem, dbDestino);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex, "Falha crítica ao migrar banco de dados local.");
                        }
                    }
                }

                // Migração de Configurações
                string cfgNome = "backup_config.txt";
                string cfgOrigem = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, cfgNome);
                string cfgDestino = Path.Combine(appDataFolder, cfgNome);
                
                if (!File.Exists(cfgDestino))
                {
#if !DEBUG
                    // Tenta do antigo (somente em RELEASE)
                    string cfgAntigo = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "GestaoAulas", cfgNome);
                    if (File.Exists(cfgAntigo))
                    {
                        try { File.Copy(cfgAntigo, cfgDestino); } catch { }
                    }
                    else if (File.Exists(cfgOrigem))
#else
                    if (File.Exists(cfgOrigem))
#endif
                    {
                        try { File.Copy(cfgOrigem, cfgDestino); } catch { }
                    }
                }

                // Cria pasta de Exports localmente (como estava antes)
                Directory.CreateDirectory(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "exports"));

                // Inicializa Banco de Dados
                var repo = AppHost.Services.GetRequiredService<IAulaRepository>();
                await repo.InicializarBancoAsync().ConfigureAwait(true);

                // Faz backup inicial de segurança
                var backupMgr = AppHost.Services.GetRequiredService<BackupManager>();
                await backupMgr.CriarBackupAsync().ConfigureAwait(true);

                // Resolve a Janela Principal
                var startupForm = AppHost.Services.GetRequiredService<MainWindow>();
                startupForm.Show();
                
                base.OnStartup(e);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Falha crítica na inicialização");
                MessageBox.Show($"Erro fatal na inicialização: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            Log.Information(">>> Aplicação Encerrando <<<");
            
            try 
            {
                if (AppHost != null)
                {
                    // Removido backup forçado no fechamento (Causa travamentos se o disco estiver ocupado)
                    // O backup já é feito na inicialização e o usuário pode fazer manual.
                    
                    AppHost.StopAsync(TimeSpan.FromSeconds(5)).GetAwaiter().GetResult();
                    AppHost.Dispose();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Erro durante o encerramento planejado");
            }
            finally
            {
                Log.CloseAndFlush();
                _mutex?.ReleaseMutex();
                _mutex?.Dispose();
            }

            base.OnExit(e);
        }
    }
}
