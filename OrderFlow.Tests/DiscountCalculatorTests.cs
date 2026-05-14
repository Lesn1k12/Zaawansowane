using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class DiscountCalculatorTests
{
    // ── Rule 1 ─────────────────────────────────────────────────────────────────

    [Fact]
    public void Calculate_StandardCustomerSmallAmount_ReturnsZeroDiscount()
    {
        // Arrange
        var calculator = new DiscountCalculator();
        var customer   = new Customer("Bob", "Warsaw", isVIP: false);
        const decimal orderTotal = 500m;

        // Act
        decimal discount = calculator.Calculate(customer, orderTotal);

        // Assert
        Assert.Equal(0m, discount);
    }
}
