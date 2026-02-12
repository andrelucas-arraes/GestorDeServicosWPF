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
        Task<List<Aula>> ObterTodasAsync(int? mes = null, int? ano = null, string? termoBusca = null);
        Task<List<int>> ObterAnosDisponiveisAsync();
        
        Task BackupDatabaseAsync(string destinationPath);
    }
}
