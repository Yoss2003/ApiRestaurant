using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonqoAPI.Service.IService
{
    public interface ITableService
    {
        Task<IEnumerable<Table>> GetAllActiveTablesAsync(int restaurantId);
        Task<Table> CreateTableAsync(TableCreateDTO dto, int restaurantId);
        Task<Table?> UpdateTableAsync(int id, TableCreateDTO dto, int restaurantId);
        Task<bool> DeleteTableAsync(int id, int restaurantId);
    }
}