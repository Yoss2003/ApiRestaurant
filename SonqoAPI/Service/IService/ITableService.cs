using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;

namespace SonqoAPI.Service.IService
{
    public interface ITableService
    {
        Task<IEnumerable<Table>> GetAllActiveTablesAsync();
        Task<Table> CreateTableAsync(TableCreateDTO dto);
        Task<Table?> UpdateTableAsync(int id, TableCreateDTO dto);
        Task<bool> DeleteTableAsync(int id);
    }
}