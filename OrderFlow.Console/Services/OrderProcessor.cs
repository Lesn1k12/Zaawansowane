using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderProcessor
{
    private readonly List<Order> _orders;

    public OrderProcessor(List<Order> orders) => _orders = orders;

    public void Run()
    {
        System.Console.WriteLine("\n=== TASK 3 — Action, Func, Predicate ===\n");

        Predicate<Order> isCompleted   = o => o.Status == OrderStatus.Completed;
        Predicate<Order> isHighValue   = o => o.TotalAmount > 1_000m;
        Predicate<Order> isVipCustomer = o => o.Customer.IsVIP;

        System.Console.WriteLine("-- Predicate<Order> filters --");

        System.Console.WriteLine("  Completed orders:");
        _orders.FindAll(isCompleted)
               .ForEach(o => System.Console.WriteLine("    " + o));

        System.Console.WriteLine("  High-value orders (> 1000):");
        _orders.FindAll(isHighValue)
               .ForEach(o => System.Console.WriteLine("    " + o));

        System.Console.WriteLine("  VIP customer orders:");
        _orders.FindAll(isVipCustomer)
               .ForEach(o => System.Console.WriteLine("    " + o));

        Action<Order> printOrder = o =>
            System.Console.WriteLine($"    {o.Customer.Name,-20} | {o.TotalAmount,10:C} | {o.Status}");

        Action<Order> markProcessing = o =>
        {
            if (o.Status == OrderStatus.Validated)
            {
                o.Status = OrderStatus.Processing;
                System.Console.WriteLine($"    STATUS CHANGED: {o.Customer.Name}'s order -> Processing");
            }
        };

        System.Console.WriteLine("\n-- Action<Order> usages --");
        System.Console.WriteLine("  All orders (print action):");
        _orders.ForEach(printOrder);

        System.Console.WriteLine("  Promoting Validated -> Processing:");
        _orders.ForEach(markProcessing);

        Func<Order, object> project = o => new
        {
            Customer  = o.Customer.Name,
            City      = o.Customer.City,
            IsVIP     = o.Customer.IsVIP,
            Total     = o.TotalAmount,
            Items     = o.Items.Count,
            Status    = o.Status.ToString(),
        };

        System.Console.WriteLine("\n-- Func<Order, T> — anonymous type projection --");
        foreach (var o in _orders)
            System.Console.WriteLine("    " + project(o));

        Func<IEnumerable<Order>, decimal> sumAgg = orders => orders.Sum(o => o.TotalAmount);
        Func<IEnumerable<Order>, decimal> avgAgg = orders => orders.Average(o => o.TotalAmount);
        Func<IEnumerable<Order>, decimal> maxAgg = orders => orders.Max(o => o.TotalAmount);

        decimal Aggregate(Func<IEnumerable<Order>, decimal> aggregator, IEnumerable<Order> src)
            => aggregator(src);

        var active = _orders.Where(o => o.Status != OrderStatus.Cancelled).ToList();

        System.Console.WriteLine("\n-- Aggregation (active orders) --");
        System.Console.WriteLine($"    Sum total:     {Aggregate(sumAgg, active):C}");
        System.Console.WriteLine($"    Average total: {Aggregate(avgAgg, active):C}");
        System.Console.WriteLine($"    Max total:     {Aggregate(maxAgg, active):C}");

        System.Console.WriteLine("\n-- Pipeline: filter(VIP + high-value) -> sort desc -> top 3 -> print --");
        int topN = 3;

        Predicate<Order> pipeFilter = o => o.Customer.IsVIP && o.TotalAmount > 100m;
        Func<Order, decimal> sortKey = o => o.TotalAmount;
        Action<Order> pipePrint = o =>
            System.Console.WriteLine($"    {o.Customer.Name,-20} | {o.TotalAmount,10:C} | {o.Status}");

        _orders
            .Where(o => pipeFilter(o))
            .OrderByDescending(sortKey)
            .Take(topN)
            .ToList()
            .ForEach(pipePrint);
    }
}
