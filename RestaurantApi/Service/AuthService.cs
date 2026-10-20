using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Identity;
using RestaurantApi.Models;
using RestaurantApi.Models.DTOs;
using RestaurantApi.Models.Enum;
using RestaurantApi.Repositories.IRepositories;
using RestaurantApi.Service.IService;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RestaurantApi.Service
{
    public class AuthService(
        IGenericRepository<User> userRepository,
        IGenericRepository<Restaurant> restaurantRepository,
        IConfiguration configuration,
        IPasswordHasher<User> passwordHasher) : IAuthService
    {
        public async Task<bool> RegisterAsync(RegisterDTO registerDto, int roleIdActor = 0)
        {
            if (registerDto.Role == 99 && roleIdActor != 99)
            {
                return false;
            }

            var usernameLower = registerDto.Username.ToLower();
            var exists = await userRepository.ExistsAsync(u => u.Username.ToLower() == usernameLower);
            if (exists) return false;

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

        public async Task<(LoginResponseDTO? Result, string ErrorMessage)> LoginAsync(LoginRequestDTO loginDto)
        {
            var users = await userRepository.GetAllAsync(u => u.Username.ToLower() == loginDto.Username.ToLower());
            var user = users.FirstOrDefault();

            if (user == null)
                return (null, "El usuario ingresado no existe.");

            if (!user.IsActive)
                return (null, "Este usuario ha sido desactivado por el administrador.");

            var verificationResult = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, loginDto.Password);
            if (verificationResult == PasswordVerificationResult.Failed)
                return (null, "La contraseña es incorrecta.");

            int activeRestaurantId = user.RestaurantId ?? 0;

            if (user.Role != Role.SuperAdmin)
            {
                if (string.IsNullOrWhiteSpace(loginDto.Ruc))
                    return (null, "El RUC de la empresa es obligatorio para su cuenta.");

                var rests = await restaurantRepository.GetAllAsync(r => r.Ruc == loginDto.Ruc);
                var targetRest = rests.FirstOrDefault();

                if (targetRest == null)
                    return (null, "El RUC ingresado no existe en nuestro sistema.");

                if (!targetRest.IsActive)
                    return (null, "La empresa asociada a este RUC se encuentra suspendida o inactiva.");

                if (user.RestaurantId != targetRest.Id)
                    return (null, "Usted no pertenece a la empresa vinculada a este RUC.");

                activeRestaurantId = targetRest.Id;
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(loginDto.Ruc))
                {
                    var rests = await restaurantRepository.GetAllAsync(r => r.Ruc == loginDto.Ruc);
                    var targetRest = rests.FirstOrDefault();

                    if (targetRest != null)
                        activeRestaurantId = targetRest.Id;
                    else
                        return (null, "SuperAdmin: El RUC ingresado no existe. Verifíquelo para administrar esa empresa.");
                }
            }

            var token = GenerateJwtToken(user, activeRestaurantId);

            return (new LoginResponseDTO
            {
                Token = token,
                Role = user.Role.ToString()!
            }, string.Empty);
        }

        private string GenerateJwtToken(User user, int restaurantId)
        {
            var secretKey = configuration["JwtSettings:SecretKey"];
            var issuer = configuration["JwtSettings:Issuer"] ?? "RestaurantApi";
            var audience = configuration["JwtSettings:Audience"] ?? "SonqoAdmin";
            var key = Encoding.UTF8.GetBytes(secretKey!);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim("RestaurantId", restaurantId.ToString())
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(8),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(key),
                    SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        // --- MÉTODOS DEL CRUD DE USUARIOS (Con RestaurantId) ---

        public async Task<IEnumerable<UserResponseDTO>> GetAllUsersAsync(int restaurantId)
        {
            var users = await userRepository.GetAllAsync(u => u.IsActive && u.RestaurantId == restaurantId);
            return users.Select(u => new UserResponseDTO
            {
                Id = u.Id,
                Username = u.Username,
                Role = u.Role.ToString()
            });
        }

        public async Task<UserResponseDTO?> CreateUserAsync(UserCreateDTO dto, int adminRestaurantId, string adminRole)
        {
            var usernameLower = dto.Username!.ToLower();
            var exists = await userRepository.ExistsAsync(u => u.Username.ToLower() == usernameLower);
            if (exists) return null;

            if (!Enum.TryParse<Role>(dto.Role, out var roleEnum))
            {
                roleEnum = Role.Kitchen;
            }

            if ((roleEnum == Role.SuperAdmin || roleEnum == Role.Admin) && adminRole != "SuperAdmin")
            {
                return null;
            }

            var newUser = new User
            {
                Username = dto.Username!,
                Role = roleEnum,
                IsActive = true,
                RestaurantId = adminRestaurantId
            };

            newUser.PasswordHash = passwordHasher.HashPassword(newUser, dto.Password!);

            await userRepository.AddAsync(newUser);
            await userRepository.SaveChangesAsync();

            return new UserResponseDTO { Id = newUser.Id, Username = newUser.Username, Role = newUser.Role.ToString() };
        }

        public async Task<bool> UpdateUserAsync(int id, UserUpdateDTO dto, int adminRestaurantId, string adminRole)
        {
            var user = await userRepository.GetByIdAsync(id);
            if (user == null || !user.IsActive || user.RestaurantId != adminRestaurantId) return false;

            if ((user.Role == Role.SuperAdmin || user.Role == Role.Admin) && adminRole != "SuperAdmin")
            {
                return false;
            }

            if (Enum.TryParse<Role>(dto.Role, out var roleEnum))
            {
                if ((roleEnum == Role.SuperAdmin || roleEnum == Role.Admin) && adminRole != "SuperAdmin")
                {
                    return false;
                }
                user.Role = roleEnum;
            }

            var usernameLower = dto.Username!.ToLower();
            var exists = await userRepository.ExistsAsync(u => u.Username.ToLower() == usernameLower && u.Id != id);
            if (exists) return false;

            user.Username = dto.Username!;

            if (!string.IsNullOrWhiteSpace(dto.Password))
            {
                user.PasswordHash = passwordHasher.HashPassword(user, dto.Password);
            }

            userRepository.Update(user);
            await userRepository.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteUserAsync(int id, int restaurantId, string adminRole)
        {
            var user = await userRepository.GetByIdAsync(id);
            if (user == null || user.RestaurantId != restaurantId) return false;

            if ((user.Role == Role.SuperAdmin || user.Role == Role.Admin) && adminRole != "SuperAdmin") return false;

            if (user.Username.Equals("admin", StringComparison.CurrentCultureIgnoreCase)) return false;

            user.IsActive = false;
            userRepository.Update(user);
            await userRepository.SaveChangesAsync();
            return true;
        }
    }
}