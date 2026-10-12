using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
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
        IConfiguration configuration,
        IPasswordHasher<User> passwordHasher) : IAuthService
    {
        public async Task<bool> RegisterAsync(RegisterDTO registerDto)
        {
            var existingUsers = await userRepository.GetAllAsync(u => u.Username == registerDto.Username);
            if (existingUsers.Any()) return false;

            Role parsedRole = Enum.IsDefined(typeof(Role), registerDto.Role)
                              ? (Role)registerDto.Role
                              : Role.Kitchen;

            var newUser = new User
            {
                Username = registerDto.Username,
                Role = parsedRole,
                IsActive = true,
                RestaurantId = registerDto.RestaurantId
            };

            newUser.PasswordHash = passwordHasher.HashPassword(newUser, registerDto.Password);

            await userRepository.AddAsync(newUser);
            return await userRepository.SaveChangesAsync();
        }

        public async Task<LoginResponseDTO?> LoginAsync(LoginRequestDTO loginDto)
        {
            var users = await userRepository.GetAllAsync(u => u.Username == loginDto.Username && u.IsActive);
            var user = users.FirstOrDefault();

            if (user == null) return null;

            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (verificationResult == PasswordVerificationResult.Failed) return null;

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