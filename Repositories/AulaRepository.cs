using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Data;
using Microsoft.Data.Sqlite;
using Dapper;
using Dapper.Contrib.Extensions;
using GestaoAulas.Models;
using Microsoft.Extensions.Logging;

namespace GestaoAulas.Repositories
{
    /// <summary>
    /// Repositório de Aulas usando Dapper.
    /// </summary>
    public class AulaRepository : IAulaRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<AulaRepository> _logger;

        public AulaRepository(ILogger<AulaRepository> logger)
        {
            _logger = logger;
#if DEBUG
            string pasta = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DevData");
#else
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string pasta = Path.Combine(appData, "GestorDeServicos");
#endif
            
            if (!Directory.Exists(pasta))
                Directory.CreateDirectory(pasta);

            string dbPath = Path.Combine(pasta, "aulas.db");
            _connectionString = $"Data Source={dbPath}";
            
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            SqlMapper.AddTypeHandler(new GestaoAulas.Infrastructure.DecimalTypeHandler());
        }

        private SqliteConnection GetConnection() => new SqliteConnection(_connectionString);

        public async Task InicializarBancoAsync()
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                var sql = @"
                    PRAGMA journal_mode=WAL;
                    CREATE TABLE IF NOT EXISTS aulas (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        data TEXT NOT NULL,
                        dia_semana TEXT NOT NULL,
                        nome_aula TEXT NOT NULL,
                        tag TEXT NOT NULL DEFAULT '',
                        duracao REAL NOT NULL,
                        valor REAL NOT NULL,
                        valor_hora REAL NOT NULL DEFAULT 0,
                        categoria TEXT NOT NULL DEFAULT 'Aula',
                        status TEXT NOT NULL DEFAULT 'Pendente',
                        data_criacao TEXT NOT NULL,
                        data_atualizacao TEXT NOT NULL
                    );
                    CREATE TABLE IF NOT EXISTS categorias (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        nome TEXT NOT NULL UNIQUE
                    );";
                
                await conn.ExecuteAsync(sql).ConfigureAwait(false);

                // Migração de colunas existentes
                var existingColumns = (await conn.QueryAsync<string>(
                    "SELECT name FROM pragma_table_info('aulas')").ConfigureAwait(false)).AsList();

                if (!existingColumns.Contains("valor_hora"))
                    await conn.ExecuteAsync("ALTER TABLE aulas ADD COLUMN valor_hora REAL NOT NULL DEFAULT 0").ConfigureAwait(false);

                if (!existingColumns.Contains("categoria"))
                    await conn.ExecuteAsync("ALTER TABLE aulas ADD COLUMN categoria TEXT NOT NULL DEFAULT 'Aula'").ConfigureAwait(false);

                if (!existingColumns.Contains("tag"))
                    await conn.ExecuteAsync("ALTER TABLE aulas ADD COLUMN tag TEXT NOT NULL DEFAULT ''").ConfigureAwait(false);

                // Popula categorias padrão se a tabela estiver vazia
                var count = await conn.ExecuteScalarAsync<int>("SELECT COUNT(*) FROM categorias").ConfigureAwait(false);
                if (count == 0)
                {
                    var categPadrao = new[] { "Aula", "Serviço", "Freelance", "Manutenção", "Outro" };
                    foreach (var c in categPadrao)
                    {
                        await conn.ExecuteAsync("INSERT OR IGNORE INTO categorias (nome) VALUES (@Nome)", new { Nome = c }).ConfigureAwait(false);
                    }
                    _logger.LogInformation("Categorias padrão inseridas no banco.");
                }

                _logger.LogInformation("Banco de dados inicializado com sucesso em: {Path}", _connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Erro fatal ao inicializar banco de dados.");
                throw;
            }
        }

        public async Task<int> InserirAsync(Aula aula)
        {
            try
            {
                _logger.LogDebug("Inserindo aula: Data={Data}, Nome={Nome}, Valor={Valor}, Status={Status}",
                    aula.Data, aula.NomeAula, aula.Valor, aula.Status);
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                var sql = @"
                    INSERT INTO aulas (data, dia_semana, nome_aula, tag, duracao, valor, valor_hora, categoria, status, data_criacao, data_atualizacao)
                    VALUES (@Data, @DiaSemana, @NomeAula, @Tag, @Duracao, @Valor, @ValorHora, @Categoria, @Status, @DataCriacao, @DataAtualizacao);
                    SELECT last_insert_rowid();";
                
                var parametros = new
                {
                    Data = aula.Data.ToString("yyyy-MM-dd"),
                    DiaSemana = aula.DiaSemana,
                    NomeAula = aula.NomeAula,
                    Tag = aula.Tag ?? "",
                    Duracao = aula.Duracao,
                    Valor = aula.Valor.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ValorHora = aula.ValorHora.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Categoria = aula.Categoria,
                    Status = aula.Status,
                    DataCriacao = aula.DataCriacao.ToString("yyyy-MM-dd HH:mm:ss"),
                    DataAtualizacao = aula.DataAtualizacao.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                var id = await conn.ExecuteScalarAsync<int>(sql, parametros).ConfigureAwait(false);
                aula.Id = id;
                _logger.LogInformation("Aula inserida com sucesso. ID={Id}", id);
                return id;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao inserir aula no banco.");
                throw;
            }
        }

        public async Task<bool> AtualizarAsync(Aula aula)
        {
            try
            {
                _logger.LogDebug("Atualizando aula ID={Id}", aula.Id);
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                aula.DataAtualizacao = DateTime.Now;
                
                var sql = @"
                    UPDATE aulas SET 
                        data = @Data, 
                        dia_semana = @DiaSemana, 
                        nome_aula = @NomeAula, 
                        tag = @Tag,
                        duracao = @Duracao, 
                        valor = @Valor, 
                        valor_hora = @ValorHora,
                        categoria = @Categoria,
                        status = @Status, 
                        data_atualizacao = @DataAtualizacao
                    WHERE id = @Id";
                
                var parametros = new
                {
                    Id = aula.Id,
                    Data = aula.Data.ToString("yyyy-MM-dd"),
                    DiaSemana = aula.DiaSemana,
                    NomeAula = aula.NomeAula,
                    Tag = aula.Tag ?? "",
                    Duracao = aula.Duracao,
                    Valor = aula.Valor.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    ValorHora = aula.ValorHora.ToString(System.Globalization.CultureInfo.InvariantCulture),
                    Categoria = aula.Categoria,
                    Status = aula.Status,
                    DataAtualizacao = aula.DataAtualizacao.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                var affected = await conn.ExecuteAsync(sql, parametros).ConfigureAwait(false);
                bool sucesso = affected > 0;
                
                if (sucesso)
                    _logger.LogInformation("Aula atualizada. ID={Id}", aula.Id);
                else
                    _logger.LogWarning("Aula não encontrada para atualização. ID={Id}", aula.Id);
                    
                return sucesso;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao atualizar aula ID={Id}", aula.Id);
                throw;
            }
        }

        public async Task<bool> ExcluirAsync(int id)
        {
            try
            {
                _logger.LogDebug("Excluindo aula ID={Id}", id);
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                var affected = await conn.ExecuteAsync("DELETE FROM aulas WHERE id = @Id", new { Id = id }).ConfigureAwait(false);
                bool sucesso = affected > 0;
                
                if (sucesso)
                    _logger.LogInformation("Aula excluída. ID={Id}", id);
                else
                    _logger.LogWarning("Aula não encontrada para exclusão. ID={Id}", id);
                    
                return sucesso;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao excluir aula ID={Id}", id);
                throw;
            }
        }

        public async Task<Aula?> ObterPorIdAsync(int id)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                var sql = "SELECT * FROM aulas WHERE id = @Id";
                return await conn.QueryFirstOrDefaultAsync<Aula>(sql, new { Id = id }).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar aula por ID={Id}", id);
                throw;
            }
        }

        public async Task<List<Aula>> ObterTodasAsync(int? mes = null, int? ano = null, string? termoBusca = null, string? filtroStatus = null)
        {
            try
            {
                _logger.LogDebug("Buscando aulas. Filtros: Mes={Mes}, Ano={Ano}, Busca={Busca}, Status={Status}", mes, ano, termoBusca, filtroStatus);
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                var sql = "SELECT * FROM aulas WHERE 1=1";
                var parametros = new DynamicParameters();

                if (ano.HasValue)
                {
                    sql += " AND strftime('%Y', data) = @Ano";
                    parametros.Add("@Ano", ano.Value.ToString("D4"));
                }

                if (mes.HasValue)
                {
                    sql += " AND strftime('%m', data) = @Mes";
                    parametros.Add("@Mes", mes.Value.ToString("D2"));
                }

                if (!string.IsNullOrWhiteSpace(termoBusca))
                {
                    sql += " AND (nome_aula LIKE @Busca OR tag LIKE @Busca OR data LIKE @Busca)";
                    parametros.Add("@Busca", $"%{termoBusca}%");
                }

                if (!string.IsNullOrWhiteSpace(filtroStatus) && filtroStatus != "Todos")
                {
                    sql += " AND status = @FiltroStatus";
                    parametros.Add("@FiltroStatus", filtroStatus);
                }

                sql += " ORDER BY data DESC";

                var result = await conn.QueryAsync<Aula>(sql, parametros).ConfigureAwait(false);
                var lista = result.AsList();
                
                _logger.LogInformation("Aulas carregadas. Total={Count}", lista.Count);
                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar aulas.");
                throw;
            }
        }

        public async Task<List<int>> ObterAnosDisponiveisAsync()
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                var sql = "SELECT DISTINCT strftime('%Y', data) FROM aulas ORDER BY 1 DESC";
                var result = await conn.QueryAsync<string>(sql).ConfigureAwait(false);
                
                var anos = new List<int>();
                foreach (var anoStr in result)
                {
                    if (int.TryParse(anoStr, out int ano))
                        anos.Add(ano);
                }
                
                return anos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar anos disponíveis");
                throw;
            }
        }

        public async Task BackupDatabaseAsync(string destinationPath)
        {
            try
            {
                var fullPath = Path.GetFullPath(destinationPath);
                if (!fullPath.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                    throw new ArgumentException("O caminho de backup deve terminar com .db");
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                var safePath = fullPath.Replace("'", "''");
                await conn.ExecuteAsync($"VACUUM INTO '{safePath}'").ConfigureAwait(false);
                _logger.LogInformation("Backup realizado. Destino={Destination}", fullPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar backup para: {Path}", destinationPath);
                throw;
            }
        }

        public async Task<decimal> ObterTotalPendenteAnoAsync(int? ano)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                var sql = "SELECT COALESCE(SUM(CAST(valor AS REAL)), 0) FROM aulas WHERE status = 'Pendente'";
                var parametros = new DynamicParameters();

                if (ano.HasValue)
                {
                    sql += " AND strftime('%Y', data) = @Ano";
                    parametros.Add("@Ano", ano.Value.ToString("D4"));
                }

                var result = await conn.ExecuteScalarAsync<double>(sql, parametros).ConfigureAwait(false);
                return (decimal)result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao calcular total pendente do ano {Ano}", ano);
                return 0m;
            }
        }

        // --- Categorias ---

        public async Task<List<string>> ObterCategoriasAsync()
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                var result = await conn.QueryAsync<string>("SELECT nome FROM categorias ORDER BY id").ConfigureAwait(false);
                return result.AsList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar categorias");
                return new List<string> { "Aula", "Serviço", "Freelance", "Manutenção", "Outro" };
            }
        }

        public async Task AdicionarCategoriaAsync(string categoria)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(categoria)) return;
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                await conn.ExecuteAsync("INSERT OR IGNORE INTO categorias (nome) VALUES (@Nome)", new { Nome = categoria.Trim() }).ConfigureAwait(false);
                _logger.LogInformation("Categoria adicionada: {Categoria}", categoria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar categoria: {Categoria}", categoria);
                throw;
            }
        }

        public async Task RemoverCategoriaAsync(string categoria)
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                await conn.ExecuteAsync("DELETE FROM categorias WHERE nome = @Nome", new { Nome = categoria }).ConfigureAwait(false);
                _logger.LogInformation("Categoria removida: {Categoria}", categoria);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover categoria: {Categoria}", categoria);
                throw;
            }
        }
    }
}
