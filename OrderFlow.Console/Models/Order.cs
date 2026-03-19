namespace OrderFlow.Console.Models;

public class Order
{
    public Customer Customer { get; set; }
    public DateTime Date { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItem> Items { get; set; }
    public decimal TotalAmount => Items?.Sum(i => i.TotalPrice) ?? 0m;

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
