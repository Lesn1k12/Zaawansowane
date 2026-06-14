using Xunit;
using Moq;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderCurrencyConverterTests
{
    private static Order MakeOrder(decimal pricePerItem, int quantity = 1)
    {
        var customer = new Customer("Alice", "Warsaw", false);
        var product  = new Product("Widget", pricePerItem, "General");
        var items    = new List<OrderItem> { new(product, quantity) };
        return new Order(customer, DateTime.Today, OrderStatus.New, items);
    }

    // ── Test 1: Known currency → converted total ───────────────────────────────

    [Fact]
    public async Task ConvertOrderTotalAsync_KnownCurrency_ReturnsOrderTotalDividedByRate()
    {
        // Arrange
        // Order total: 2 × 500 PLN = 1 000 PLN
        // USD rate: 4.00 PLN/USD  →  1 000 / 4.00 = 250 USD
        var mockService = new Mock<ICurrencyService>();
        mockService
            .Setup(s => s.GetRateAsync("USD"))
            .ReturnsAsync(4.00m);

        var converter = new OrderCurrencyConverter(mockService.Object);
        var order     = MakeOrder(pricePerItem: 500m, quantity: 2);

        // Act
        decimal? result = await converter.ConvertOrderTotalAsync(order, "USD");

        // Assert
        Assert.Equal(250m, result);
        mockService.Verify(s => s.GetRateAsync("USD"), Times.Once);
    }

    // ── Test 2: Unknown currency → null ───────────────────────────────────────

    [Fact]
    public async Task ConvertOrderTotalAsync_UnknownCurrency_ReturnsNull()
    {
        // Arrange
        var mockService = new Mock<ICurrencyService>();
        mockService
            .Setup(s => s.GetRateAsync("XYZ"))
            .ReturnsAsync((decimal?)null);

        var converter = new OrderCurrencyConverter(mockService.Object);
        var order     = MakeOrder(pricePerItem: 200m);

        // Act
        decimal? result = await converter.ConvertOrderTotalAsync(order, "XYZ");

        // Assert
        Assert.Null(result);
    }
}
