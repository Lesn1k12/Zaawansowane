using Xunit;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class OrderValidatorTests
{
    // ── Helpers ────────────────────────────────────────────────────────────────

    private static Customer MakeCustomer() => new("Alice", "Warsaw", false);

    private static OrderItem MakeItem(decimal price = 100m, int quantity = 1)
        => new(new Product("Widget", price, "General"), quantity);

    private static Order ValidOrder(
        DateTime? date = null,
        OrderStatus status = OrderStatus.New,
        List<OrderItem>? items = null)
        => new(
            MakeCustomer(),
            date ?? DateTime.Today,
            status,
            items ?? new List<OrderItem> { MakeItem() });

    private readonly OrderValidator _sut = new();

    // ── Named Rule: HasItems ───────────────────────────────────────────────────

    [Fact]
    public void ValidateAll_OrderWithNoItems_ReturnsHasItemsError()
    {
        // Arrange
        var order = new Order(MakeCustomer(), DateTime.Today, OrderStatus.New, new List<OrderItem>());

        // Act
        var (isValid, errors) = _sut.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("at least one item"));
    }

    // ── Named Rule: TotalUnderLimit ────────────────────────────────────────────

    [Fact]
    public void ValidateAll_TotalExceedsLimit_ReturnsTotalLimitError()
    {
        // Arrange
        // 11 items × $1,000 each = $11,000, which is above the $10,000 cap
        var items = Enumerable.Range(1, 11)
            .Select(_ => MakeItem(price: 1_000m, quantity: 1))
            .ToList();
        var order = ValidOrder(items: items);

        // Act
        var (isValid, errors) = _sut.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("exceeds the limit"));
    }

    // ── Named Rule: AllQuantitiesPositive ─────────────────────────────────────

    [Fact]
    public void ValidateAll_ItemWithZeroQuantity_ReturnsQuantityError()
    {
        // Arrange
        var items = new List<OrderItem> { MakeItem(quantity: 0) };
        var order = ValidOrder(items: items);

        // Act
        var (isValid, errors) = _sut.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("greater than zero"));
    }

    // ── Lambda Rule: Date not in the future ───────────────────────────────────

    [Fact]
    public void ValidateAll_OrderDateInFuture_ReturnsFutureDateError()
    {
        // Arrange
        var order = ValidOrder(date: DateTime.Today.AddDays(1));

        // Act
        var (isValid, errors) = _sut.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("not be in the future"));
    }

    // ── Lambda Rule: Status is not Cancelled ──────────────────────────────────

    [Fact]
    public void ValidateAll_CancelledOrder_ReturnsCancelledStatusError()
    {
        // Arrange
        var order = ValidOrder(status: OrderStatus.Cancelled);

        // Act
        var (isValid, errors) = _sut.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Contains(errors, e => e.Contains("Cancelled"));
    }

    // ── ValidateAll: multiple rules broken simultaneously ─────────────────────

    [Fact]
    public void ValidateAll_NoItemsFutureDateCancelledStatus_ReturnsThreeErrors()
    {
        // Arrange — breaks HasItems, FutureDate, and CancelledStatus rules
        var order = new Order(
            MakeCustomer(),
            date: DateTime.Today.AddDays(7),
            status: OrderStatus.Cancelled,
            items: new List<OrderItem>());

        // Act
        var (isValid, errors) = _sut.ValidateAll(order);

        // Assert
        Assert.False(isValid);
        Assert.Equal(3, errors.Count);
        Assert.Contains(errors, e => e.Contains("at least one item"));
        Assert.Contains(errors, e => e.Contains("not be in the future"));
        Assert.Contains(errors, e => e.Contains("Cancelled"));
    }

    // ── Theory: status validation ──────────────────────────────────────────────

    [Theory]
    [InlineData(OrderStatus.New,        true)]
    [InlineData(OrderStatus.Validated,  true)]
    [InlineData(OrderStatus.Processing, true)]
    [InlineData(OrderStatus.Completed,  true)]
    [InlineData(OrderStatus.Cancelled,  false)]
    public void ValidateAll_StatusVariants_OnlyCancelledFailsStatusRule(
        OrderStatus status, bool expectedValid)
    {
        // Arrange — all other rules satisfied; only the status differs
        var order = ValidOrder(status: status);

        // Act
        var (isValid, errors) = _sut.ValidateAll(order);

        // Assert
        Assert.Equal(expectedValid, isValid);
        if (!expectedValid)
            Assert.Contains(errors, e => e.Contains("Cancelled"));
    }
}
