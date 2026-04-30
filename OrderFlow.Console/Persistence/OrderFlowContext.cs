using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public class OrderFlowContext : DbContext
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder
            .UseSqlite("Data Source=orderflow.db")
            .LogTo(System.Console.WriteLine, LogLevel.Information);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Customer ──────────────────────────────────────────────────────
        // [NotMapped] na Name wystarczy, ale jawne Ignore() jest czytelniejsze
        modelBuilder.Entity<Customer>()
            .Ignore(c => c.Name);

        // Customer 1:N Order — DeleteBehavior.Restrict
        modelBuilder.Entity<Customer>()
            .HasMany(c => c.Orders)
            .WithOne(o => o.Customer)
            .HasForeignKey(o => o.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Order ─────────────────────────────────────────────────────────
        // Ignoruj właściwość obliczaną
        modelBuilder.Entity<Order>()
            .Ignore(o => o.TotalAmount);

        // Order 1:N OrderItem — DeleteBehavior.Cascade
        modelBuilder.Entity<Order>()
            .HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        // ── OrderItem ─────────────────────────────────────────────────────
        // Ignoruj właściwość obliczaną
        modelBuilder.Entity<OrderItem>()
            .Ignore(oi => oi.TotalPrice);

        // OrderItem N:1 Product
        modelBuilder.Entity<OrderItem>()
            .HasOne(oi => oi.Product)
            .WithMany()
            .HasForeignKey(oi => oi.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        // ── Precyzja decimal ──────────────────────────────────────────────
        modelBuilder.Entity<Product>()
            .Property(p => p.Price)
            .HasPrecision(18, 2);

        // ── Indeksy ───────────────────────────────────────────────────────
        modelBuilder.Entity<Customer>()
            .HasIndex(c => c.FullName)
            .HasDatabaseName("IX_Customers_FullName");

        modelBuilder.Entity<Order>()
            .HasIndex(o => o.Status)
            .HasDatabaseName("IX_Orders_Status");
    }
}
