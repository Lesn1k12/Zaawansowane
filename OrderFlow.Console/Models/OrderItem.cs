namespace OrderFlow.Console.Models;

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }

    public decimal TotalPrice => Product.Price * Quantity;

    public OrderItem() { }

    public OrderItem(Product product, int quantity)
    {
        Product = product;
        ProductId = product?.Id ?? 0;
        Quantity = quantity;
    }

    public override string ToString() =>
        $"    {Product.Name} x{Quantity} = {TotalPrice:C}";
}
