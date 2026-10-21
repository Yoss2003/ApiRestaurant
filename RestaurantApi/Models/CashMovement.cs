using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RestaurantApi.Models
{
    public class CashMovement
    {
        [Key]
        public int Id { get; set; }

        public int CashRegisterId { get; set; }
        public CashRegister? CashRegister { get; set; }

        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [MaxLength(10)]
        public string Type { get; set; } = string.Empty;

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Required]
        [MaxLength(200)]
        public string Reason { get; set; } = string.Empty;

        public DateTime MovementDate { get; set; } = DateTime.UtcNow;
        public int PaymentMethodId { get; set; } = 1;
        public PaymentMethod? PaymentMethod { get; set; }
    }
}