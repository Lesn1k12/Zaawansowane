using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class OrderCurrencyConverter
{
    private readonly ICurrencyService _currency;

    public OrderCurrencyConverter(ICurrencyService currency) => _currency = currency;

    /// <summary>Returns the order's PLN total expressed in <paramref name="targetCurrency"/>,
    /// or <c>null</c> when the target currency code is unknown.</summary>
    public async Task<decimal?> ConvertOrderTotalAsync(Order order, string targetCurrency)
    {
        // Order totals are stored in PLN; GetRateAsync("PLN") == 1, so fromRate is always 1.
        var rate = await _currency.GetRateAsync(targetCurrency);
        if (rate is null) return null;

        return order.TotalAmount / rate.Value;
    }
}
