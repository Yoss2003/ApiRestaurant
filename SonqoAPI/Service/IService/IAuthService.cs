using SonqoAPI.Models.DTOs;

namespace SonqoAPI.Service.IService
{
    public interface IAuthService
    {
        Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO loginDto);
        Task<bool> RegisterAsync(RegisterDTO registerDto);
        Task<IEnumerable<UserResponseDTO>> GetAllUsersAsync(int restaurantId);
        Task<UserResponseDTO?> CreateUserAsync(UserCreateDTO dto, int restaurantId);
        Task<bool> UpdateUserAsync(int id, UserUpdateDTO dto, int restaurantId);
        Task<bool> DeleteUserAsync(int id, int restaurantId);
    }
}
