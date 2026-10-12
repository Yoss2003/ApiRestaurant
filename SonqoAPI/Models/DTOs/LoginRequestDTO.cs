namespace SonqoAPI.Models.DTOs
{
    // Lo que envía el frontend para iniciar sesión
    public class LoginRequestDTO
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    // Lo que responde la API si las credenciales son correctas
    public class LoginResponseDTO
    {
        public string Token { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
    }
}