using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

public class Order
{
    public int Id { get; set; }
    public int CustomerId { get; set; }

    [JsonPropertyName("customer")]
    public Customer Customer { get; set; } = null!;

    [XmlElement("OrderDate")]
    public DateTime Date { get; set; }

    [XmlAttribute]
    public OrderStatus Status { get; set; }

    public string? Notes { get; set; }

    public List<OrderItem> Items { get; set; } = new List<OrderItem>();

    [JsonIgnore]
    [XmlIgnore]
    public decimal TotalAmount => Items?.Sum(i => i.TotalPrice) ?? 0m;

    public Order() { }

    public Order(Customer customer, DateTime date, OrderStatus status,
                 List<OrderItem> items, string? notes = null)
    {
        Customer = customer;
        CustomerId = customer?.Id ?? 0;
        Date = date;
        Status = status;
        Items = items ?? new List<OrderItem>();
        Notes = notes;
    }

    public override string ToString() =>
        $"[{Status,-12}] {Customer?.Name ?? "?",-20} {Date:yyyy-MM-dd}  Total: {TotalAmount,10:C}";
}
