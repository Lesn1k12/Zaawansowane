using System.Text.Json.Serialization;

namespace OrderFlow.Console.Models;

public class NbpResponse
{
    [JsonPropertyName("table")]
    public string Table { get; set; } = string.Empty;

    [JsonPropertyName("currency")]
    public string Currency { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public string Code { get; set; } = string.Empty;

    [JsonPropertyName("rates")]
    public List<NbpRate> Rates { get; set; } = new();
}

public class NbpRate
{
    [JsonPropertyName("no")]
    public string No { get; set; } = string.Empty;

    [JsonPropertyName("effectiveDate")]
    public string EffectiveDate { get; set; } = string.Empty;

    [JsonPropertyName("mid")]
    public decimal Mid { get; set; }
}
