using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;

namespace SonqoAPI.Service
{
    public class TableService(IGenericRepository<Table> tableRepository) : ITableService
    {
        public async Task<IEnumerable<Table>> GetAllActiveTablesAsync()
        {
            // Filtra directamente las que no están eliminadas
            return await tableRepository.GetAllAsync(t => t.IsDeleted == false);
        }

        public async Task<Table> CreateTableAsync(TableCreateDTO dto)
        {
            var newTable = new Table
            {
                Number = dto.Number,
                Capacity = dto.Capacity,
                IsAvailable = true,
                IsDeleted = false
            };

            await tableRepository.AddAsync(newTable);
            await tableRepository.SaveChangesAsync();

            return newTable;
        }

        public async Task<Table?> UpdateTableAsync(int id, TableCreateDTO dto)
        {
            var table = await tableRepository.GetByIdAsync(id);

            if (table == null || table.IsDeleted)
                return null;

            table.Number = dto.Number;
            table.Capacity = dto.Capacity;

            tableRepository.Update(table);
            await tableRepository.SaveChangesAsync();

            return table;
        }

        public async Task<bool> DeleteTableAsync(int id)
        {
            var table = await tableRepository.GetByIdAsync(id);

            if (table == null || table.IsDeleted)
                return false;

            if (!table.IsAvailable)
                throw new InvalidOperationException("No puedes eliminar una mesa que está actualmente ocupada.");

            // Aplicamos el borrado lógico
            table.IsDeleted = true;
            table.IsAvailable = false;

            tableRepository.Update(table);
            await tableRepository.SaveChangesAsync();

            return true;
        }
    }
}