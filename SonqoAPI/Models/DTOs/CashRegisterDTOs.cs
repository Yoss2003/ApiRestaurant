using System.ComponentModel.DataAnnotations;

namespace SonqoAPI.Models.DTOs
{
    public class OpenRegisterDTO
    {
        [Required]
        [Range(0, 100000, ErrorMessage = "El monto inicial debe ser válido.")]
        public decimal OpeningBalance { get; set; }
    }

    public class CloseRegisterDTO
    {
        [Required]
        public decimal ActualBalance { get; set; }
        public string? Notes { get; set; }
    }
}