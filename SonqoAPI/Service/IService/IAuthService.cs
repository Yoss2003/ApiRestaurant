using SonqoAPI.Models.DTOs;

namespace SonqoAPI.Service.IService
{
    public interface IAuthService
    {
        Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO loginDto);
        Task<bool> RegisterAsync(RegisterDTO registerDto);
    }
}
