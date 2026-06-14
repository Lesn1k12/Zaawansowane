using Xunit;
using System.Net;
using OrderFlow.Console.Services;
using OrderFlow.Tests.Helpers;

namespace OrderFlow.Tests;

public class CurrencyServiceTests
{
    // ── Shared NBP JSON stubs ──────────────────────────────────────────────────

    private static string NbpJson(string code, decimal mid) => $$"""
        {
          "table": "A",
          "currency": "test currency",
          "code": "{{code}}",
          "rates": [
            {
              "no": "001/A/NBP/2025",
              "effectiveDate": "2025-05-14",
              "mid": {{mid.ToString(System.Globalization.CultureInfo.InvariantCulture)}}
            }
          ]
        }
        """;

    private static CurrencyService Build(TestHttpMessageHandler handler)
        => new(new HttpClient(handler));

    // ── Test 1: Happy path ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_ValidCode_ReturnsMidRateFromJson()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            _ => TestHttpMessageHandler.Json(NbpJson("USD", 3.92m)));
        var sut = Build(handler);

        // Act
        decimal? rate = await sut.GetRateAsync("USD");

        // Assert
        Assert.Equal(3.92m, rate);
    }

    // ── Test 2: PLN shortcut — no network call must be made ───────────────────

    [Fact]
    public async Task GetRateAsync_PlnCode_ReturnsOneWithoutNetworkCall()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            _ => TestHttpMessageHandler.Status(HttpStatusCode.OK)); // should never fire
        var sut = Build(handler);

        // Act
        decimal? rate = await sut.GetRateAsync("PLN");

        // Assert
        Assert.Equal(1.0m, rate);
        Assert.Equal(0, handler.CallCount); // no HTTP request made
    }

    // ── Test 3: 404 → null ────────────────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_UnknownCode_Returns404AsNull()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            _ => TestHttpMessageHandler.Status(HttpStatusCode.NotFound));
        var sut = Build(handler);

        // Act
        decimal? rate = await sut.GetRateAsync("XYZ");

        // Assert
        Assert.Null(rate);
    }

    // ── Test 4: 500 → CurrencyServiceException ────────────────────────────────

    [Fact]
    public async Task GetRateAsync_ServerError_ThrowsCurrencyServiceExceptionWithStatusCode()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            _ => TestHttpMessageHandler.Status(HttpStatusCode.InternalServerError));
        var sut = Build(handler);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<CurrencyServiceException>(
            () => sut.GetRateAsync("USD"));

        Assert.Equal(500, ex.StatusCode);
        Assert.Contains("USD", ex.Message);
    }

    // ── Test 5: Correct URL is called ─────────────────────────────────────────

    [Fact]
    public async Task GetRateAsync_ValidCode_CallsCorrectNbpUrl()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            _ => TestHttpMessageHandler.Json(NbpJson("EUR", 4.25m)));
        var sut = Build(handler);

        // Act
        await sut.GetRateAsync("eur"); // lowercase on input

        // Assert — URL must contain uppercase code and the NBP path
        var url = handler.LastRequest!.RequestUri!.ToString();
        Assert.Contains("/exchangerates/rates/A/EUR/", url);
        Assert.Contains("format=json", url);
    }

    // ── Test 7: Cache — second call must not hit the network ──────────────────

    [Fact]
    public async Task GetRateAsync_CalledTwiceForSameCode_OnlyOneHttpRequestIsMade()
    {
        // Arrange
        var handler = new TestHttpMessageHandler(
            _ => TestHttpMessageHandler.Json(NbpJson("USD", 3.92m)));
        var sut = Build(handler);

        // Act
        decimal? first  = await sut.GetRateAsync("USD");
        decimal? second = await sut.GetRateAsync("USD"); // must be served from cache

        // Assert
        Assert.Equal(3.92m, first);
        Assert.Equal(3.92m, second);
        Assert.Equal(1, handler.CallCount); // handler fired exactly once
    }

    // ── Test 6: ConvertAsync — cross-currency via PLN base ────────────────────

    [Fact]
    public async Task ConvertAsync_UsdToEur_ConvertsCorrectlyViaPln()
    {
        // Arrange
        // USD rate: 4.00 PLN  |  EUR rate: 5.00 PLN
        // 100 USD → 400 PLN → 80 EUR
        var handler = new TestHttpMessageHandler(request =>
        {
            var url = request.RequestUri!.ToString();
            if (url.Contains("USD")) return TestHttpMessageHandler.Json(NbpJson("USD", 4.00m));
            if (url.Contains("EUR")) return TestHttpMessageHandler.Json(NbpJson("EUR", 5.00m));
            return TestHttpMessageHandler.Status(HttpStatusCode.NotFound);
        });
        var sut = Build(handler);

        // Act
        decimal result = await sut.ConvertAsync(100m, "USD", "EUR");

        // Assert
        Assert.Equal(80m, result);
    }
}
