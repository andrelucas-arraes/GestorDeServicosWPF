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
            // Configura o caminho do banco. Em DEBUG, usa pasta local "DevData". Em RELEASE, usa AppData.
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
            
            // Tenta mapear snake_case (banco) para PascalCase (C#)
            Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
            
            // Registra handler para conversão de decimais (correção para valores vindo como 0)
            SqlMapper.AddTypeHandler(new GestaoAulas.Infrastructure.DecimalTypeHandler());
        }

        private SqliteConnection GetConnection() => new SqliteConnection(_connectionString);

        public async Task InicializarBancoAsync()
        {
            try
            {
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);

                // Cria tabela se não existir (SQL manual ainda necessário para DDL)
                var sql = @"
                    PRAGMA journal_mode=WAL;
                    CREATE TABLE IF NOT EXISTS aulas (
                        id INTEGER PRIMARY KEY AUTOINCREMENT,
                        data TEXT NOT NULL,
                        dia_semana TEXT NOT NULL,
                        nome_aula TEXT NOT NULL,
                        duracao REAL NOT NULL,
                        valor REAL NOT NULL,
                        valor_hora REAL NOT NULL DEFAULT 0,
                        categoria TEXT NOT NULL DEFAULT 'Aula',
                        status TEXT NOT NULL DEFAULT 'Pendente',
                        data_criacao TEXT NOT NULL,
                        data_atualizacao TEXT NOT NULL
                    )";
                
                await conn.ExecuteAsync(sql).ConfigureAwait(false);

                // Migração rápida
                try {
                    await conn.ExecuteAsync("ALTER TABLE aulas ADD COLUMN valor_hora REAL NOT NULL DEFAULT 0").ConfigureAwait(false);
                } catch { /* Já existe */ }

                try {
                    await conn.ExecuteAsync("ALTER TABLE aulas ADD COLUMN categoria TEXT NOT NULL DEFAULT 'Aula'").ConfigureAwait(false);
                } catch { /* Já existe */ }

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
                _logger.LogDebug("Iniciando inserção de aula: Data={Data}, Nome={Nome}, Duracao={Duracao}, Valor={Valor}, ValorHora={ValorHora}, Status={Status}",
                    aula.Data, aula.NomeAula, aula.Duracao, aula.Valor, aula.ValorHora, aula.Status);
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                var sql = @"
                    INSERT INTO aulas (data, dia_semana, nome_aula, duracao, valor, valor_hora, categoria, status, data_criacao, data_atualizacao)
                    VALUES (@Data, @DiaSemana, @NomeAula, @Duracao, @Valor, @ValorHora, @Categoria, @Status, @DataCriacao, @DataAtualizacao);
                    SELECT last_insert_rowid();";
                
                var parametros = new
                {
                    Data = aula.Data.ToString("yyyy-MM-dd"),
                    DiaSemana = aula.DiaSemana,
                    NomeAula = aula.NomeAula,
                    Duracao = aula.Duracao,
                    // Removido cast para double para manter precisão (Dapper decide o melhor tipo, geralmente REAL ou NUMERIC no SQLite)
                    Valor = aula.Valor,
                    ValorHora = aula.ValorHora,
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
                _logger.LogDebug("Iniciando atualização de aula ID={Id}", aula.Id);
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                aula.DataAtualizacao = DateTime.Now;
                
                var sql = @"
                    UPDATE aulas SET 
                        data = @Data, 
                        dia_semana = @DiaSemana, 
                        nome_aula = @NomeAula, 
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
                    Duracao = aula.Duracao,
                    // Removido cast para double
                    Valor = aula.Valor,
                    ValorHora = aula.ValorHora,
                    Categoria = aula.Categoria,
                    Status = aula.Status,
                    DataAtualizacao = aula.DataAtualizacao.ToString("yyyy-MM-dd HH:mm:ss")
                };
                
                var affected = await conn.ExecuteAsync(sql, parametros).ConfigureAwait(false);
                bool sucesso = affected > 0;
                
                if (sucesso)
                    _logger.LogInformation("Aula atualizada com sucesso. ID={Id}", aula.Id);
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
                _logger.LogDebug("Iniciando exclusão de aula ID={Id}", id);
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                var affected = await conn.ExecuteAsync("DELETE FROM aulas WHERE id = @Id", new { Id = id }).ConfigureAwait(false);
                bool sucesso = affected > 0;
                
                if (sucesso)
                    _logger.LogInformation("Aula excluída com sucesso. ID={Id}", id);
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
                _logger.LogDebug("Buscando aula por ID={Id}", id);
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                var sql = "SELECT * FROM aulas WHERE id = @Id";
                var aula = await conn.QueryFirstOrDefaultAsync<Aula>(sql, new { Id = id }).ConfigureAwait(false);
                
                if (aula != null)
                    _logger.LogDebug("Aula encontrada. ID={Id}, Nome={Nome}", id, aula.NomeAula);
                else
                    _logger.LogDebug("Aula não encontrada. ID={Id}", id);
                    
                return aula;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar aula por ID={Id}", id);
                throw;
            }
        }

        public async Task<List<Aula>> ObterTodasAsync(int? mes = null, int? ano = null, string? termoBusca = null)
        {
            try
            {
                _logger.LogDebug("Buscando aulas. Filtros: Mes={Mes}, Ano={Ano}, Busca={Busca}", mes, ano, termoBusca);
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                // Base query com Dapper Builder ou SQL dinâmico
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
                    sql += " AND (nome_aula LIKE @Busca OR data LIKE @Busca)";
                    parametros.Add("@Busca", $"%{termoBusca}%");
                }

                sql += " ORDER BY data DESC";

                var result = await conn.QueryAsync<Aula>(sql, parametros).ConfigureAwait(false);
                var lista = result.AsList();
                
                _logger.LogInformation("Aulas carregadas. Total={Count}, Mes={Mes}, Ano={Ano}", lista.Count, mes, ano);
                return lista;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar aulas. Filtros: Mes={Mes}, Ano={Ano}, Busca={Busca}", mes, ano, termoBusca);
                throw;
            }
        }

        public async Task<List<int>> ObterAnosDisponiveisAsync()
        {
            try
            {
                _logger.LogDebug("Buscando anos disponíveis no banco");
                
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
                
                _logger.LogDebug("Anos disponíveis: {Anos}", string.Join(", ", anos));
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
                _logger.LogDebug("Iniciando backup do banco para: {Path}", destinationPath);
                
                using var conn = GetConnection();
                await conn.OpenAsync().ConfigureAwait(false);
                
                // Sanitiza o path para evitar problemas com aspas simples no caminho
                var safePath = destinationPath.Replace("'", "''");
                await conn.ExecuteAsync($"VACUUM INTO '{safePath}'").ConfigureAwait(false);
                _logger.LogInformation("Backup realizado com sucesso. Destino={Destination}", destinationPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao realizar backup para: {Path}", destinationPath);
                throw;
            }
        }
    }
}
