using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class DiscountCalculator
{
    private const decimal VipDiscountRate       = 0.10m;
    private const decimal HighValueDiscountRate = 0.05m;
    private const decimal VipHighValueBonusRate = 0.05m;
    private const decimal MaxDiscountRate       = 0.25m;
    private const decimal HighValueThreshold    = 1_000m;
    private const decimal VipHighValueThreshold = 5_000m;

    public decimal Calculate(Customer customer, decimal orderTotal)
    {
        var rate = Math.Min(BaseRate(customer, orderTotal), MaxDiscountRate);
        return orderTotal * rate;
    }

    private static decimal BaseRate(Customer customer, decimal orderTotal)
    {
        var rate = 0m;

        if (customer.IsVIP)
            rate += VipDiscountRate;

        if (orderTotal > HighValueThreshold)
            rate += HighValueDiscountRate;

        if (customer.IsVIP && orderTotal > VipHighValueThreshold)
            rate += VipHighValueBonusRate;

        return rate;
    }
}
