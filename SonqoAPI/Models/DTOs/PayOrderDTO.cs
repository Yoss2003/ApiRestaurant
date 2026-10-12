using System.ComponentModel.DataAnnotations;

namespace SonqoAPI.Models.DTOs
{
    public class PayOrderDTO
    {
        public int PrimaryPaymentMethodId { get; set; }

        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "El monto a pagar debe ser mayor a cero.")]
        public decimal TotalAmountPaid { get; set; }
        public List<SplitPaymentDTO> SplitPayments { get; set; } = [];
    }

    public class SplitPaymentDTO
    {
        [Range(1, int.MaxValue)]
        public int PaymentMethodId { get; set; }

        [Range(0.01, (double)decimal.MaxValue, ErrorMessage = "El monto dividido debe ser mayor a cero.")]
        public decimal Amount { get; set; }
    }
}