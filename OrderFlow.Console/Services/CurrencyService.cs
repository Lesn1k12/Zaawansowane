using System.Net;
using System.Net.Http.Json;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public class CurrencyService : ICurrencyService
{
    private const string BaseUrl = "https://api.nbp.pl/api/exchangerates/rates/A";

    private readonly HttpClient _http;

    public CurrencyService(HttpClient http) => _http = http;

    public async Task<decimal?> GetRateAsync(string currencyCode)
    {
        if (currencyCode.Equals("PLN", StringComparison.OrdinalIgnoreCase))
            return 1.0m;

        var url = $"{BaseUrl}/{currencyCode.ToUpperInvariant()}/?format=json";
        var response = await _http.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        if (!response.IsSuccessStatusCode)
            throw new CurrencyServiceException(
                $"Failed to fetch rate for '{currencyCode}'. HTTP {(int)response.StatusCode}.",
                (int)response.StatusCode);

        var data = await response.Content.ReadFromJsonAsync<NbpResponse>();
        return data?.Rates.FirstOrDefault()?.Mid;
    }

    public async Task<decimal> ConvertAsync(decimal amount, string fromCurrency, string toCurrency)
    {
        var fromRate = await GetRateAsync(fromCurrency)
            ?? throw new CurrencyServiceException(
                $"Cannot convert: rate not found for '{fromCurrency}'.", 404);

        var toRate = await GetRateAsync(toCurrency)
            ?? throw new CurrencyServiceException(
                $"Cannot convert: rate not found for '{toCurrency}'.", 404);

        // Both rates are expressed as PLN per 1 unit of foreign currency.
        // amount (in fromCurrency) → PLN → toCurrency
        return amount * fromRate / toRate;
    }
}
