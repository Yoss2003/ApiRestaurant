namespace RestaurantApi.Service.IService
{
    public interface IAuditService
    {
        Task LogActionAsync(int restaurantId, int userId, string actionType, string entityName, string entityId, string description);
        Task<IEnumerable<AuditLogResponseDTO>> GetRecentLogsAsync(int restaurantId, int limit = 50);
    }
}