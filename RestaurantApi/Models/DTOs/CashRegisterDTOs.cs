using System.ComponentModel.DataAnnotations;

namespace RestaurantApi.Models.DTOs
{
    public class OpenRegisterDTO
    {
        [Required]
        [Range(0, 100000, ErrorMessage = "El monto inicial debe ser válido.")]
        public decimal OpeningBalance { get; set; }
    }

    public class OpenRegisterInfoDTO
    {
        public int RegisterId { get; set; }
        public int UserId { get; set; }
        public decimal OpeningBalance { get; set; }
        public DateTime OpenedAt { get; set; }
        public decimal ExpectedCashBalance { get; set; }
        public object Movements { get; set; } = new List<object>();
        public object PaymentBreakdown { get; set; } = new List<object>();
    }

    public class UpdateMovementReasonDTO
    {
        [Required]
        public string Reason { get; set; } = string.Empty;
    }

    public class CloseRegisterDTO
    {
        [Required]
        public decimal ActualBalance { get; set; }
        public string? Notes { get; set; }
    }

    public class CashMovementCreateDTO
    {
        [Required]
        [RegularExpression("IN|OUT", ErrorMessage = "El tipo debe ser IN (Ingreso) o OUT (Egreso).")]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Range(0.01, 10000, ErrorMessage = "Monto inválido.")]
        public decimal Amount { get; set; }

        [Required]
        public string Reason { get; set; } = string.Empty;
        public int PaymentMethodId { get; set; } = 1;
    }
}