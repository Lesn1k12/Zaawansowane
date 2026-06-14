using Xunit;
using OrderFlow.Console.Models;
using OrderFlow.Console.Services;

namespace OrderFlow.Tests;

public class DiscountCalculatorTests
{
    private static Customer Standard() => new("Bob",   "Warsaw", isVIP: false);
    private static Customer Vip()      => new("Alice", "Warsaw", isVIP: true);

    // ── Rule 1: standard customer, small amount → 0% ──────────────────────────

    [Fact]
    public void Calculate_StandardCustomerSmallAmount_ReturnsZeroDiscount()
    {
        // Arrange
        var calculator = new DiscountCalculator();
        const decimal orderTotal = 500m;

        // Act
        decimal discount = calculator.Calculate(Standard(), orderTotal);

        // Assert
        Assert.Equal(0m, discount);
    }

    // ── Rule 2: VIP customer → 10% ────────────────────────────────────────────

    [Fact]
    public void Calculate_VipCustomerSmallAmount_ReturnsTenPercentDiscount()
    {
        // Arrange
        var calculator = new DiscountCalculator();
        const decimal orderTotal = 500m;

        // Act
        decimal discount = calculator.Calculate(Vip(), orderTotal);

        // Assert — 10% of 500 = 50 PLN
        Assert.Equal(50m, discount);
    }

    // ── Rule 3: order over 1000 zł → +5% ─────────────────────────────────────

    [Fact]
    public void Calculate_StandardCustomerHighValueOrder_ReturnsFivePercentDiscount()
    {
        // Arrange
        var calculator = new DiscountCalculator();
        const decimal orderTotal = 1_500m;

        // Act
        decimal discount = calculator.Calculate(Standard(), orderTotal);

        // Assert — 5% of 1500 = 75 PLN
        Assert.Equal(75m, discount);
    }

    [Fact]
    public void Calculate_StandardCustomerExactlyAtThreshold_ReturnsZeroDiscount()
    {
        // Arrange — exactly 1000 is NOT above the threshold
        var calculator = new DiscountCalculator();
        const decimal orderTotal = 1_000m;

        // Act
        decimal discount = calculator.Calculate(Standard(), orderTotal);

        // Assert
        Assert.Equal(0m, discount);
    }

    // ── Rule 4: VIP + order over 5000 zł → +5% bonus (20% total) ────────────

    [Fact]
    public void Calculate_VipCustomerHighValueOrder_ReturnsFifteenPercentDiscount()
    {
        // Arrange — VIP(10%) + high-value(5%) = 15%, no VIP bonus (under 5000)
        var calculator = new DiscountCalculator();
        const decimal orderTotal = 1_500m;

        // Act
        decimal discount = calculator.Calculate(Vip(), orderTotal);

        // Assert — 15% of 1500 = 225 PLN
        Assert.Equal(225m, discount);
    }

    [Fact]
    public void Calculate_VipCustomerOrderAbove5000_ReturnsTwentyPercentDiscount()
    {
        // Arrange — VIP(10%) + high-value(5%) + VIP bonus(5%) = 20%
        var calculator = new DiscountCalculator();
        const decimal orderTotal = 6_000m;

        // Act
        decimal discount = calculator.Calculate(Vip(), orderTotal);

        // Assert — 20% of 6000 = 1200 PLN
        Assert.Equal(1_200m, discount);
    }

    [Fact]
    public void Calculate_StandardCustomerOrderAbove5000_ReturnsFivePercentOnly()
    {
        // Arrange — VIP bonus must NOT apply to non-VIP
        var calculator = new DiscountCalculator();
        const decimal orderTotal = 6_000m;

        // Act
        decimal discount = calculator.Calculate(Standard(), orderTotal);

        // Assert — 5% of 6000 = 300 PLN
        Assert.Equal(300m, discount);
    }

    // ── Rule 5: max discount 25% regardless of rules met ─────────────────────

    [Fact]
    public void Calculate_AnyOrder_DiscountNeverExceedsTwentyFivePercent()
    {
        // Arrange — the cap must hold for arbitrarily large VIP orders
        var calculator = new DiscountCalculator();
        const decimal orderTotal = 100_000m;

        // Act
        decimal discount = calculator.Calculate(Vip(), orderTotal);

        // Assert — current max is 20% (< cap), cap prevents future rule creep
        Assert.True(discount <= orderTotal * 0.25m,
            "Discount must never exceed 25% of order total");
    }

    // ── Rule 6: discount returned as PLN amount, not percent ─────────────────

    [Fact]
    public void Calculate_VipOrderAbove5000_ReturnsPlnAmountNotPercent()
    {
        // Arrange
        var calculator = new DiscountCalculator();
        const decimal orderTotal = 6_000m;

        // Act
        decimal discount = calculator.Calculate(Vip(), orderTotal);

        // Assert — 1200 PLN, NOT the rate 0.20
        Assert.Equal(1_200m, discount);
        Assert.NotEqual(0.20m, discount);
    }

    // ── Theory: multiple customer/amount combinations ─────────────────────────

    [Theory]
    [InlineData(false, 500,    0)]     // standard, small      → 0%
    [InlineData(true,  500,   50)]     // VIP, small           → 10%
    [InlineData(false, 2000, 100)]     // standard, high-value → 5%
    [InlineData(true,  2000, 300)]     // VIP, high-value      → 15%
    [InlineData(true,  6000, 1200)]    // VIP, over 5000       → 20%
    public void Calculate_VariousCustomerAndAmountCombinations_ReturnsCorrectPlnDiscount(
        bool isVip, double orderTotalInput, double expectedDiscountInput)
    {
        // Arrange
        var calculator     = new DiscountCalculator();
        var customer       = new Customer("Test", "Warsaw", isVip);
        var orderTotal     = (decimal)orderTotalInput;
        var expectedAmount = (decimal)expectedDiscountInput;

        // Act
        decimal discount = calculator.Calculate(customer, orderTotal);

        // Assert
        Assert.Equal(expectedAmount, discount);
    }
}
