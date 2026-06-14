using Xunit;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderProcessorTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Order MakeOrder(
        string customerName,
        OrderStatus status,
        decimal pricePerItem,
        int quantity = 1,
        bool isVip = false)
    {
        var customer = new Customer(customerName, "Warsaw", isVip);
        var product  = new Product("Product", pricePerItem, "General");
        var items    = new List<OrderItem> { new(product, quantity) };
        return new Order(customer, DateTime.Today, status, items);
    }

    // ── Predicate-based filtering ──────────────────────────────────────────────

    [Fact]
    public void FilterOrders_CompletedPredicate_ReturnsOnlyCompletedOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            MakeOrder("Alice",   OrderStatus.Completed,  500m),
            MakeOrder("Bob",     OrderStatus.Processing, 200m),
            MakeOrder("Charlie", OrderStatus.Completed,  800m),
            MakeOrder("Diana",   OrderStatus.Cancelled,  100m),
        };
        var processor = new OrderProcessor(orders);
        Predicate<Order> isCompleted = o => o.Status == OrderStatus.Completed;

        // Act
        var result = processor.FilterOrders(isCompleted);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.All(result, o => Assert.Equal(OrderStatus.Completed, o.Status));
    }

    // ── Data aggregation: sum of active order totals ───────────────────────────

    [Fact]
    public void CalculateTotalAmount_ActiveOrders_ExcludesCancelledOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            MakeOrder("Alice",   OrderStatus.Completed,  300m, quantity: 2),  // 600
            MakeOrder("Bob",     OrderStatus.Validated,  150m, quantity: 1),  // 150
            MakeOrder("Charlie", OrderStatus.Cancelled, 1000m, quantity: 5),  // excluded
        };
        var processor   = new OrderProcessor(orders);
        var activeOrders = orders.Where(o => o.Status != OrderStatus.Cancelled);
        const decimal expected = 750m; // 600 + 150

        // Act
        decimal actual = processor.CalculateTotalAmount(activeOrders);

        // Assert
        Assert.Equal(expected, actual);
    }
}
