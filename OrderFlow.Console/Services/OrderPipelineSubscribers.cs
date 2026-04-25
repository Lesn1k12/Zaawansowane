using OrderFlow.Console.Events;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public static class ConsoleLogger
{
    public static void OnStatusChanged(object? sender, OrderStatusChangedEventArgs e) =>
        System.Console.WriteLine(
            $"  [LOG {e.Timestamp:HH:mm:ss.fff}] {e.Order.Customer.Name}: {e.OldStatus} → {e.NewStatus}");
}

public static class EmailNotifier
{
    public static void OnStatusChanged(object? sender, OrderStatusChangedEventArgs e) =>
        System.Console.WriteLine(
            $"  [EMAIL] Sending email: order for {e.Order.Customer.Name} is now {e.NewStatus}");
}

public class StatisticsTracker
{
    private readonly Dictionary<OrderStatus, int> _counts = new();

    public void OnStatusChanged(object? sender, OrderStatusChangedEventArgs e)
    {
        _counts.TryGetValue(e.NewStatus, out var current);
        _counts[e.NewStatus] = current + 1;
    }

    public void PrintReport()
    {
        System.Console.WriteLine("\n  [STATS] Orders reached each status:");
        foreach (var (status, count) in _counts.OrderBy(kv => kv.Key))
            System.Console.WriteLine($"    {status,-12}: {count}");
    }
}
