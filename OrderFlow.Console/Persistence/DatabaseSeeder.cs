using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(OrderFlowContext db)
    {
        // Sprawdź, czy baza jest już wypełniona — jeśli tak, nic nie rób
        if (await db.Products.AnyAsync()) return;

        // ── 5 produktów ───────────────────────────────────────────────────
        var products = new List<Product>
        {
            new("Laptop Pro",      3499.99m, "Electronics", stock: 50),
            new("Wireless Mouse",    49.99m, "Electronics", stock: 200),
            new("Office Chair",     799.00m, "Furniture",   stock: 30),
            new("Java Book",         89.99m, "Books",       stock: 100),
            new("C# in Depth",       79.99m, "Books",       stock: 80),
        };
        await db.Products.AddRangeAsync(products);

        // ── 4 klientów ────────────────────────────────────────────────────
        var customers = new List<Customer>
        {
            new("Alice Kowalski",   "Warsaw", isVIP: true,  email: "alice@example.com"),
            new("Bob Nowak",        "Krakow", isVIP: false, email: "bob@example.com"),
            new("Carol Wisniewski", "Gdansk", isVIP: true,  email: null),
            new("David Zajac",      "Warsaw", isVIP: false, email: "david@example.com"),
        };
        await db.Customers.AddRangeAsync(customers);

        // SaveChanges — EF przypisuje Id, zanim stworzymy zamówienia
        await db.SaveChangesAsync();

        // ── 6 zamówień ────────────────────────────────────────────────────
        var orders = new List<Order>
        {
            new Order
            {
                CustomerId = customers[0].Id,
                Date       = new DateTime(2026, 1, 10),
                Status     = OrderStatus.Completed,
                Items      =
                [
                    new() { ProductId = products[0].Id, Quantity = 1 },
                    new() { ProductId = products[1].Id, Quantity = 2 },
                ],
            },
            new Order
            {
                CustomerId = customers[0].Id,
                Date       = new DateTime(2026, 2, 5),
                Status     = OrderStatus.Processing,
                Notes      = "Pilne zamówienie VIP",
                Items      =
                [
                    new() { ProductId = products[4].Id, Quantity = 1 },
                    new() { ProductId = products[3].Id, Quantity = 1 },
                ],
            },
            new Order
            {
                CustomerId = customers[1].Id,
                Date       = new DateTime(2026, 1, 20),
                Status     = OrderStatus.Validated,
                Items      =
                [
                    new() { ProductId = products[2].Id, Quantity = 1 },
                ],
            },
            new Order
            {
                CustomerId = customers[2].Id,
                Date       = new DateTime(2026, 3, 1),
                Status     = OrderStatus.New,
                Items      =
                [
                    new() { ProductId = products[4].Id, Quantity = 3 },
                ],
            },
            new Order
            {
                CustomerId = customers[3].Id,
                Date       = new DateTime(2026, 2, 14),
                Status     = OrderStatus.Cancelled,
                Items      =
                [
                    new() { ProductId = products[0].Id, Quantity = 2 },
                ],
            },
            new Order
            {
                CustomerId = customers[1].Id,
                Date       = new DateTime(2026, 3, 10),
                Status     = OrderStatus.New,
                Items      =
                [
                    new() { ProductId = products[1].Id, Quantity = 5 },
                ],
            },
        };

        await db.Orders.AddRangeAsync(orders);
        await db.SaveChangesAsync();

        System.Console.WriteLine($"  [Seeder] Dodano: {products.Count} produktów, " +
                                 $"{customers.Count} klientów, {orders.Count} zamówień.");
    }
}
