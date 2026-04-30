using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;

namespace OrderFlow.Console.Services;

public static class EfLinqQueries
{
    // ── Q1: Zamówienia klientów VIP o wartości > 500 PLN ─────────────────
    public static async Task PrintVipOrdersAbove500Async(OrderFlowContext db)
    {
        System.Console.WriteLine("\n  [EF-Q1] Zamówienia VIP > 500 PLN");

        // Include ładuje Customer i Items w jednym zapytaniu
        var orders = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .Where(o => o.Customer.IsVIP)
            .ToListAsync();

        // TotalAmount jest obliczane w pamięci (Ignore), więc filtrujemy post-load
        var result = orders
            .Where(o => o.TotalAmount > 500m)
            .OrderByDescending(o => o.TotalAmount)
            .ToList();

        foreach (var o in result)
            System.Console.WriteLine($"    Id={o.Id,-3} {o.Customer.FullName,-20} " +
                                     $"{o.Status,-12} {o.TotalAmount:C}");

        if (result.Count == 0)
            System.Console.WriteLine("    (brak wyników)");
    }

    // ── Q2: Top 3 klientów wg sumy wydatków ──────────────────────────────
    public static async Task PrintTop3CustomersBySpendingAsync(OrderFlowContext db)
    {
        System.Console.WriteLine("\n  [EF-Q2] Ranking Top 3 klientów (suma wydatków)");

        var orders = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .ToListAsync();

        var ranking = orders
            .GroupBy(o => o.Customer)
            .Select(g => new
            {
                Customer   = g.Key.FullName,
                IsVIP      = g.Key.IsVIP,
                OrderCount = g.Count(),
                TotalSpent = g.Sum(o => o.TotalAmount),
            })
            .OrderByDescending(x => x.TotalSpent)
            .Take(3)
            .ToList();

        int pos = 1;
        foreach (var x in ranking)
            System.Console.WriteLine($"    #{pos++} {x.Customer,-20}{(x.IsVIP ? " *VIP*" : ""),-7} " +
                                     $"— {x.OrderCount} zam., łącznie: {x.TotalSpent:C}");
    }

    // ── Q3: Średnia wartość zamówienia per miasto ─────────────────────────
    public static async Task PrintAvgOrderValuePerCityAsync(OrderFlowContext db)
    {
        System.Console.WriteLine("\n  [EF-Q3] Średnia wartość zamówienia per miasto");

        var orders = await db.Orders
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product)
            .ToListAsync();

        var result = orders
            .GroupBy(o => o.Customer.City)
            .Select(g => new
            {
                City     = g.Key,
                Count    = g.Count(),
                AvgValue = g.Average(o => o.TotalAmount),
            })
            .OrderByDescending(x => x.AvgValue)
            .ToList();

        foreach (var x in result)
            System.Console.WriteLine($"    {x.City,-10}: {x.Count} zam., " +
                                     $"średnia = {x.AvgValue:C}");
    }

    // ── Q4: Produkty nigdy niezamówione (anti-join) ───────────────────────
    public static async Task PrintNeverOrderedProductsAsync(OrderFlowContext db)
    {
        System.Console.WriteLine("\n  [EF-Q4] Produkty nigdy niezamówione (anti-join)");

        // Budujemy IQueryable — filtr wykonuje się po stronie SQLite
        var orderedIds = db.OrderItems.Select(oi => oi.ProductId);

        var neverOrdered = await db.Products
            .Where(p => !orderedIds.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToListAsync();

        if (neverOrdered.Count == 0)
        {
            System.Console.WriteLine("    (wszystkie produkty były zamawiane)");
            return;
        }

        foreach (var p in neverOrdered)
            System.Console.WriteLine($"    {p.Name,-20} ({p.Category}) — {p.Price:C}");
    }

    // ── Q5: Dynamiczny filtr budowany warunkowo na IQueryable ─────────────
    /// <summary>
    /// Buduje zapytanie etap po etapie. Żaden warunek nie jest wymagany —
    /// każdy parametr dodaje klauzulę WHERE tylko gdy ma wartość.
    /// </summary>
    public static IQueryable<Order> BuildDynamicFilter(
        IQueryable<Order> baseQuery,
        OrderStatus? status,
        decimal? minAmount)
    {
        var query = baseQuery
            .Include(o => o.Customer)
            .Include(o => o.Items)
                .ThenInclude(oi => oi.Product);

        if (status.HasValue)
            query = query.Where(o => o.Status == status.Value);

        // minAmount filtruje po stronie DB: suma (qty * price) per zamówienie
        if (minAmount.HasValue)
        {
            var threshold = minAmount.Value;
            query = query.Where(o =>
                o.Items.Sum(oi => (decimal)oi.Quantity * oi.Product.Price) >= threshold);
        }

        return query;
    }

    public static async Task PrintDynamicFilterAsync(
        OrderFlowContext db, OrderStatus? status, decimal? minAmount)
    {
        var statusLabel = status?.ToString() ?? "dowolny";
        var amountLabel = minAmount?.ToString("C") ?? "brak";
        System.Console.WriteLine(
            $"\n  [EF-Q5] Dynamiczny filtr — Status={statusLabel}, MinAmount={amountLabel}");

        var results = await BuildDynamicFilter(db.Orders, status, minAmount)
            .OrderBy(o => o.Date)
            .ToListAsync();

        foreach (var o in results)
            System.Console.WriteLine($"    [{o.Id}] {o.Customer.FullName,-20} " +
                                     $"{o.Status,-12} {o.Date:yyyy-MM-dd} " +
                                     $"Total={o.TotalAmount:C}");

        if (results.Count == 0)
            System.Console.WriteLine("    (brak wyników)");
    }
}
