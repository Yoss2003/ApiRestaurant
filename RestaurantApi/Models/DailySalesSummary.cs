using System;
using System.ComponentModel.DataAnnotations;

namespace RestaurantApi.Models
{
    public class DailySalesSummary
    {
        [Key]
        public int Id { get; set; }
        public int RestaurantId { get; set; }
        public Restaurant? Restaurant { get; set; }

        public int DishId { get; set; }
        public Dish? Dish { get; set; }

        public DateTime Date { get; set; }
        public int TotalQuantitySold { get; set; }
    }
}