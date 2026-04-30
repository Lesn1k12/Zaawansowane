using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;

namespace OrderFlow.Console.Services;

public static class EfOrderService
{
    // ── CREATE ────────────────────────────────────────────────────────────
    /// <summary>Tworzy nowe zamówienie z dowolną liczbą pozycji.</summary>
    public static async Task<Order> CreateOrderAsync(
        OrderFlowContext db,
        int customerId,
        params (int productId, int qty)[] lines)
    {
        var order = new Order
        {
            CustomerId = customerId,
            Date       = DateTime.Now,
            Status     = OrderStatus.New,
        };

        foreach (var (productId, qty) in lines)
            order.Items.Add(new OrderItem { ProductId = productId, Quantity = qty });

        db.Orders.Add(order);
        await db.SaveChangesAsync();

        System.Console.WriteLine($"  [CREATE] Zamówienie Id={order.Id}, CustomerId={customerId}, " +
                                 $"pozycji={order.Items.Count}, Status={order.Status}");
        return order;
    }

    // ── UPDATE ────────────────────────────────────────────────────────────
    /// <summary>Zmienia status na Processing i dopisuje notatkę.</summary>
    public static async Task UpdateOrderAsync(OrderFlowContext db, int orderId, string note)
    {
        var order = await db.Orders.FindAsync(orderId)
                    ?? throw new InvalidOperationException($"Order {orderId} not found");

        order.Status = OrderStatus.Processing;
        order.Notes  = note;
        await db.SaveChangesAsync();

        System.Console.WriteLine($"  [UPDATE] Order {orderId} → Status={order.Status}, " +
                                 $"Notes=\"{order.Notes}\"");
    }

    // ── DELETE ────────────────────────────────────────────────────────────
    /// <summary>Usuwa wszystkie zamówienia ze statusem Cancelled.</summary>
    public static async Task DeleteCancelledOrdersAsync(OrderFlowContext db)
    {
        var cancelled = await db.Orders
            .Where(o => o.Status == OrderStatus.Cancelled)
            .ToListAsync();

        db.Orders.RemoveRange(cancelled);
        await db.SaveChangesAsync();

        System.Console.WriteLine($"  [DELETE] Usunięto {cancelled.Count} anulowanych zamówień.");
    }

    // ── TRANSAKCJA ────────────────────────────────────────────────────────
    /// <summary>
    /// Processing → sprawdź stock → zmniejsz stock → Completed.
    /// Przy niewystarczającym stocku: rollback + wyjątek.
    /// </summary>
    public static async Task ProcessOrderAsync(OrderFlowContext db, int orderId)
    {
        await using var transaction = await db.Database.BeginTransactionAsync();
        try
        {
            var order = await db.Orders
                .Include(o => o.Items)
                    .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.Id == orderId)
                ?? throw new InvalidOperationException($"Order {orderId} not found");

            // Krok 1 — Processing
            order.Status = OrderStatus.Processing;
            await db.SaveChangesAsync();
            System.Console.WriteLine($"  [TX] Order {orderId} → Processing");

            // Krok 2 — sprawdź stock i zmniejsz
            foreach (var item in order.Items)
            {
                if (item.Product.Stock < item.Quantity)
                    throw new InvalidOperationException(
                        $"Brak towaru: '{item.Product.Name}' " +
                        $"(dostępne={item.Product.Stock}, wymagane={item.Quantity})");

                item.Product.Stock -= item.Quantity;
                System.Console.WriteLine($"  [TX] Stock '{item.Product.Name}': " +
                                         $"{item.Product.Stock + item.Quantity} → {item.Product.Stock}");
            }

            // Krok 3 — Completed
            order.Status = OrderStatus.Completed;
            await db.SaveChangesAsync();
            await transaction.CommitAsync();
            System.Console.WriteLine($"  [TX] Order {orderId} → Completed. COMMIT.");
        }
        catch
        {
            await transaction.RollbackAsync();
            System.Console.WriteLine($"  [TX] ROLLBACK dla Order {orderId}.");
            throw;
        }
    }
}
