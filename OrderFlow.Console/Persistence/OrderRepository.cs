using System.Text.Json;
using System.Text.Encodings.Web;
using System.Xml.Serialization;
using OrderFlow.Console.Models;

namespace OrderFlow.Console.Persistence;

public class OrderRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task SaveToJsonAsync(IEnumerable<Order> orders, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        await JsonSerializer.SerializeAsync(stream, orders, JsonOptions);
    }

    public async Task<List<Order>> LoadFromJsonAsync(string path)
    {
        if (!File.Exists(path)) return new List<Order>();
        await using var stream = File.OpenRead(path);
        return await JsonSerializer.DeserializeAsync<List<Order>>(stream, JsonOptions) ?? new List<Order>();
    }

    public async Task SaveToXmlAsync(IEnumerable<Order> orders, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var stream = File.Create(path);
        var serializer = new XmlSerializer(typeof(List<Order>));
        await Task.Run(() => serializer.Serialize(stream, orders.ToList()));
    }

    public async Task<List<Order>> LoadFromXmlAsync(string path)
    {
        if (!File.Exists(path)) return new List<Order>();
        await using var stream = File.OpenRead(path);
        var serializer = new XmlSerializer(typeof(List<Order>));
        return await Task.Run(() => (List<Order>?)serializer.Deserialize(stream) ?? new List<Order>());
    }
}
