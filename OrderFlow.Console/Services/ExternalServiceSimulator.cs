using System.Diagnostics;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class ExternalServiceSimulator
{
    private static readonly Random _rng = new();

    public async Task<bool> CheckInventoryAsync(Product product)
    {
        await Task.Delay(_rng.Next(500, 1501));
        return true;
    }

    public async Task<bool> ValidatePaymentAsync(Order order)
    {
        await Task.Delay(_rng.Next(1000, 2001));
        return order.TotalAmount > 0;
    }

    public async Task<decimal> CalculateShippingAsync(Order order)
    {
        await Task.Delay(_rng.Next(300, 801));
        return Math.Round(order.TotalAmount * 0.05m, 2);
    }

    public async Task ProcessOrderAsync(Order order)
    {
        var sw = Stopwatch.StartNew();

        var inventoryTask = CheckInventoryAsync(order.Items.FirstOrDefault()?.Product
                            ?? new Product("unknown", 0, ""));
        var paymentTask   = ValidatePaymentAsync(order);
        var shippingTask  = CalculateShippingAsync(order);

        await Task.WhenAll(inventoryTask, paymentTask, shippingTask);
        sw.Stop();

        System.Console.WriteLine(
            $"  [{order.Customer.Name,-20}]  " +
            $"inventory={inventoryTask.Result}  " +
            $"payment={paymentTask.Result}  " +
            $"shipping={shippingTask.Result:C}  " +
            $"({sw.ElapsedMilliseconds} ms)");
    }

    public async Task ProcessMultipleOrdersAsync(List<Order> orders)
    {
        var semaphore = new SemaphoreSlim(3);
        int completed = 0;
        int total     = orders.Count;

        var tasks = orders.Select(async order =>
        {
            await semaphore.WaitAsync();
            try
            {
                await ProcessOrderAsync(order);
                int done = Interlocked.Increment(ref completed);
                System.Console.WriteLine($"  --> Processed {done}/{total} orders");
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}
