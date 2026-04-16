using System.Text.Json.Serialization;
using System.Xml.Serialization;

namespace OrderFlow.Console.Models;

public class Order
{
    [JsonPropertyName("customer")]
    public Customer Customer { get; set; }
m.tkhorevskyi@gmail.com
    [XmlElement("OrderDate")]
    public DateTime Date { get; set; }

    [XmlAttribute]
    public OrderStatus Status { get; set; }

    public List<OrderItem> Items { get; set; }

    [JsonIgnore]
    [XmlIgnore]
    public decimal TotalAmount => Items?.Sum(i => i.TotalPrice) ?? 0m;

    public Order()
    {
        Items = new List<OrderItem>();
    }

    public Order(Customer customer, DateTime date, OrderStatus status, List<OrderItem> items)
    {
        Customer = customer;
        Date = date;
        Status = status;
        Items = items ?? new List<OrderItem>();
    }

    public override string ToString() =>
        $"[{Status,-12}] {Customer.Name,-20} {Date:yyyy-MM-dd}  Total: {TotalAmount,10:C}";
}
