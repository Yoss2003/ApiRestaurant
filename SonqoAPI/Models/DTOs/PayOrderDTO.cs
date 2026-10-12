namespace SonqoAPI.Models.DTOs
{
    public class PayOrderDTO
    {
        public int PrimaryPaymentMethodId { get; set; }
        public decimal TotalAmountPaid { get; set; }
        public List<SplitPaymentDTO> SplitPayments { get; set; } = [];
    }

    public class SplitPaymentDTO
    {
        public int PaymentMethodId { get; set; }
        public decimal Amount { get; set; }
    }
}