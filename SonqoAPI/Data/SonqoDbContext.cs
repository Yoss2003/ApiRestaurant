using Microsoft.EntityFrameworkCore;
using SonqoAPI.Models;

namespace SonqoAPI.Data
{
    public class SonqoDbContext(DbContextOptions<SonqoDbContext> options) : DbContext(options)
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Dish> Dishes { get; set; }
        public DbSet<Ingredient> Ingredients { get; set; }
        public DbSet<RecipeDetails> RecipeDetails { get; set; }

        // Nuevos DbSets para Mesas y Pedidos
        public DbSet<Table> Tables { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderDetail> OrderDetails { get; set; }

        // DbSets para Pagos
        public DbSet<PaymentMethod> PaymentMethods { get; set; }
        public DbSet<Payment> Payments { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<RecipeDetails>()
                .HasKey(rd => new { rd.DishId, rd.IngredientId });

            modelBuilder.Entity<RecipeDetails>()
                .HasOne(rd => rd.Dish)
                .WithMany(p => p.Recipe)
                .HasForeignKey(rd => rd.DishId);

            modelBuilder.Entity<RecipeDetails>()
                .HasOne(rd => rd.Ingredient)
                .WithMany(i => i.Recipe)
                .HasForeignKey(rd => rd.IngredientId);

            // Configuraciones de nombres de tabla en PostgreSQL (todo en minúsculas es buena práctica)
            modelBuilder.Entity<User>().ToTable("usuarios");
            modelBuilder.Entity<Dish>().ToTable("platos");
            modelBuilder.Entity<Ingredient>().ToTable("ingredientes");
            modelBuilder.Entity<RecipeDetails>().ToTable("receta_detalles");
            modelBuilder.Entity<Table>().ToTable("mesas");
            modelBuilder.Entity<Order>().ToTable("pedidos");
            modelBuilder.Entity<OrderDetail>().ToTable("pedido_detalles");
            modelBuilder.Entity<PaymentMethod>().ToTable("metodos_pago");
            modelBuilder.Entity<Payment>().ToTable("pagos");

            // ==========================================
            // SEED DATA: MÉTODOS DE PAGO POR DEFECTO
            // ==========================================
            modelBuilder.Entity<PaymentMethod>().HasData(
                new PaymentMethod { Id = 1, Name = "Efectivo", IsActive = true },
                new PaymentMethod { Id = 2, Name = "Yape", IsActive = true },
                new PaymentMethod { Id = 3, Name = "Plin", IsActive = true },
                new PaymentMethod { Id = 4, Name = "Visa", IsActive = true },
                new PaymentMethod { Id = 5, Name = "Mastercard", IsActive = true }
            );
        }
    }
}