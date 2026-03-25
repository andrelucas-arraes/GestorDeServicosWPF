using System.Collections.Generic;
using System.Threading.Tasks;
using GestaoAulas.Models;

namespace GestaoAulas.Repositories
{
    public interface IAulaRepository
    {
        Task InicializarBancoAsync();
        
        Task<int> InserirAsync(Aula aula);
        Task<bool> AtualizarAsync(Aula aula);
        Task<bool> ExcluirAsync(int id);
        
        Task<Aula?> ObterPorIdAsync(int id);
        Task<List<Aula>> ObterTodasAsync(int? mes = null, int? ano = null, string? termoBusca = null, string? filtroStatus = null);
        Task<List<int>> ObterAnosDisponiveisAsync();
        Task<decimal> ObterTotalPendenteAnoAsync(int? ano);
        
        Task BackupDatabaseAsync(string destinationPath);

        // Categorias personalizáveis
        Task<List<string>> ObterCategoriasAsync();
        Task AdicionarCategoriaAsync(string categoria);
        Task RemoverCategoriaAsync(string categoria);
    }
}
