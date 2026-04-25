using System.Collections.Concurrent;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderStatistics
{
    public int TotalProcessed { get; set; }
    public decimal TotalRevenue { get; set; }
    public Dictionary<OrderStatus, int> OrdersPerStatus { get; set; } = new();
    public List<string> ProcessingErrors { get; set; } = new();

    public void Reset()
    {
        TotalProcessed = 0;
        TotalRevenue   = 0m;
        OrdersPerStatus.Clear();
        ProcessingErrors.Clear();
    }

    public void Record(Order order, IEnumerable<string> errors)
    {
        TotalProcessed++;
        TotalRevenue += order.TotalAmount;

        var status = order.Status;
        OrdersPerStatus[status] = OrdersPerStatus.TryGetValue(status, out var n) ? n + 1 : 1;

        foreach (var e in errors)
            ProcessingErrors.Add(e);
    }
}

public class OrderStatisticsSafe
{
    private int _totalProcessed;
    private decimal _totalRevenue;
    private readonly object _revenueLock = new();
    private readonly object _errorsLock  = new();

    public int TotalProcessed => _totalProcessed;
    public decimal TotalRevenue { get { lock (_revenueLock) return _totalRevenue; } }
    public ConcurrentDictionary<OrderStatus, int> OrdersPerStatus { get; } = new();
    public List<string> ProcessingErrors { get; } = new();

    public void Reset()
    {
        _totalProcessed = 0;
        lock (_revenueLock) _totalRevenue = 0m;
        OrdersPerStatus.Clear();
        lock (_errorsLock) ProcessingErrors.Clear();
    }

    public void Record(Order order, IEnumerable<string> errors)
    {
        Interlocked.Increment(ref _totalProcessed);

        lock (_revenueLock)
            _totalRevenue += order.TotalAmount;

        OrdersPerStatus.AddOrUpdate(order.Status, 1, (_, n) => n + 1);

        var errorList = errors.ToList();
        if (errorList.Count > 0)
            lock (_errorsLock)
                ProcessingErrors.AddRange(errorList);
    }
}
