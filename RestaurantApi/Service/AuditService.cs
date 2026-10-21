using Microsoft.EntityFrameworkCore;
using RestaurantApi.Models;
using RestaurantApi.Repositories.IRepositories;
using RestaurantApi.Service.IService;

namespace RestaurantApi.Service
{
    public class AuditService(IGenericRepository<AuditLog> auditRepository) : IAuditService
    {
        public async Task LogActionAsync(int restaurantId, int userId, string actionType, string entityName, string entityId, string description)
        {
            var log = new AuditLog
            {
                RestaurantId = restaurantId,
                UserId = userId,
                ActionType = actionType,
                EntityName = entityName,
                EntityId = entityId,
                Description = description,
                Timestamp = DateTime.UtcNow
            };

            await auditRepository.AddAsync(log);
            await auditRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<AuditLogResponseDTO>> GetRecentLogsAsync(int restaurantId, int limit = 50)
        {
            return await auditRepository.GetQueryable()
                .Include(a => a.User)
                .Where(a => a.RestaurantId == restaurantId)
                .OrderByDescending(a => a.Timestamp)
                .Take(limit)
                .Select(a => new AuditLogResponseDTO
                {
                    Id = a.Id,
                    Username = a.User != null ? a.User.Username : "Sistema",
                    ActionType = a.ActionType,
                    EntityName = a.EntityName,
                    Description = a.Description,
                    Timestamp = a.Timestamp
                })
                .ToListAsync();
        }
    }
}