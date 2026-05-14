namespace OrderFlow.Console.Services;

public interface ICurrencyService
{
    /// <summary>Returns the PLN mid-rate for <paramref name="currencyCode"/>,
    /// or <c>null</c> if the code is unknown (404). "PLN" always returns 1.</summary>
    Task<decimal?> GetRateAsync(string currencyCode);

    /// <summary>Converts <paramref name="amount"/> from one currency to another
    /// using PLN as the intermediate base.</summary>
    Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency);
}
