namespace RestaurantApi.Models.DTOs
{
    public class UpdateLimitsDTO 
    { 
        public int MaxUsers { get; set; } 
        public string SuperAdminPassword { get; set; } = string.Empty; 
    }
}
