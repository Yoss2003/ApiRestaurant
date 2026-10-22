using RestaurantApi.Models.DTOs;

namespace RestaurantApi.Service.IService
{
    public interface IAuthService
    {
        Task<(LoginResponseDTO? Result, string ErrorMessage)> LoginAsync(LoginRequestDTO loginDto);
        Task<bool> RegisterAsync(RegisterDTO registerDto, int roleIdActor = 0);
        Task<IEnumerable<UserResponseDTO>> GetAllUsersAsync(int restaurantId);
        Task<(UserResponseDTO? Result, string ErrorMessage)> CreateUserAsync(UserCreateDTO dto, int adminRestaurantId, string adminRole);
        Task<bool> UpdateUserAsync(int id, UserUpdateDTO dto, int adminRestaurantId, string adminRole);
        Task<bool> DeleteUserAsync(int id, int restaurantId, string adminRole);
        Task<bool> LogoutAsync(int userId);
    }
}
