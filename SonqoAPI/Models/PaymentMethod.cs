using System.ComponentModel.DataAnnotations;

namespace SonqoAPI.Models
{
    public class PaymentMethod
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [MaxLength(50)]
        public string Name { get; set; } = string.Empty;

        public bool IsActive { get; set; } = true;

        public ICollection<Payment> Payments { get; set; } = [];
    }
}