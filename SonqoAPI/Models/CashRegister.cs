using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SonqoAPI.Models
{
    public class CashRegister
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }
        public User? User { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal OpeningBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ClosingBalance { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? Discrepancy { get; set; }

        [Required]
        public DateTime OpenedAt { get; set; }

        public DateTime? ClosedAt { get; set; }

        [Required]
        public bool IsOpen { get; set; } = true;

        public string? Notes { get; set; }
        public int RestaurantId { get; set; }
        public Restaurant? Restaurant { get; set; }
    }
}