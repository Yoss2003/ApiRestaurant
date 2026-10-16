using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SonqoAPI.Service
{
    public class TableService(IGenericRepository<Table> tableRepository) : ITableService
    {
        public async Task<IEnumerable<Table>> GetAllActiveTablesAsync(int restaurantId)
        {
            return await tableRepository.GetAllAsync(t => t.IsDeleted == false && t.RestaurantId == restaurantId);
        }

        public async Task<Table> CreateTableAsync(TableCreateDTO dto, int restaurantId)
        {
            var newTable = new Table
            {
                Number = dto.Number,
                Capacity = dto.Capacity,
                RestaurantId = restaurantId,
                IsAvailable = true,
                IsDeleted = false
            };

            await tableRepository.AddAsync(newTable);
            await tableRepository.SaveChangesAsync();
            return newTable;
        }

        public async Task<Table?> UpdateTableAsync(int id, TableCreateDTO dto, int restaurantId)
        {
            var table = await tableRepository.GetByIdAsync(id);
            if (table == null || table.IsDeleted || table.RestaurantId != restaurantId)
                return null;

            table.Number = dto.Number;
            table.Capacity = dto.Capacity;
            tableRepository.Update(table);
            await tableRepository.SaveChangesAsync();
            return table;
        }

        public async Task<bool> DeleteTableAsync(int id, int restaurantId)
        {
            var table = await tableRepository.GetByIdAsync(id);
            if (table == null || table.IsDeleted || table.RestaurantId != restaurantId)
                return false;

            if (!table.IsAvailable)
                throw new InvalidOperationException("No puedes eliminar una mesa que está ocupada.");

            table.IsDeleted = true;
            table.IsAvailable = false;
            tableRepository.Update(table);
            await tableRepository.SaveChangesAsync();
            return true;
        }
    }
}