using Microsoft.IdentityModel.Tokens;
using SonqoAPI.Models;
using SonqoAPI.Models.DTOs;
using SonqoAPI.Models.Enum;
using SonqoAPI.Repositories.IRepositories;
using SonqoAPI.Service.IService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SonqoAPI.Service
{
    public class AuthService(
        IGenericRepository<User> userRepository,
        IConfiguration configuration) : IAuthService
    {
        public async Task<bool> RegisterAsync(RegisterDTO registerDto)
        {
            // 1. Verificar si el usuario ya existe
            var existingUsers = await userRepository.GetAllAsync(u => u.Username == registerDto.Username);
            if (existingUsers.Any()) return false;

            if (!Enum.TryParse<Role>(registerDto.Role, true, out var parsedRole))
            {
                parsedRole = Role.Kitchen;
            }

            // 3. Crear el nuevo usuario
            var newUser = new User
            {
                Username = registerDto.Username,
                PasswordHash = registerDto.Password,
                Role = parsedRole,
                IsActive = true
            };

            await userRepository.AddAsync(newUser);
            return await userRepository.SaveChangesAsync();
        }

        public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO loginDto)
        {
            var users = await userRepository.GetAllAsync(u => u.Username == loginDto.Username && u.IsActive);
            var user = users.FirstOrDefault();

            if (user == null || user.PasswordHash != loginDto.Password) return null;

            var token = GenerateJwtToken(user);

            return new LoginResponseDTO
            {
                Token = token,
                Role = user.Role.ToString()!
            };
        }

        private string GenerateJwtToken(User user)
        {
            var secretKey = configuration["JwtSettings:SecretKey"];
            var key = Encoding.UTF8.GetBytes(secretKey!);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("RestaurantId", user.RestaurantId?.ToString() ?? "0")
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }
    }
}