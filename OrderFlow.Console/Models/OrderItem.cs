namespace OrderFlow.Console.Models;

public class OrderItem
{
    public Product Product { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice => Product.Price * Quantity;

    public OrderItem(Product product, int quantity)
    {
        Product = product;
        Quantity = quantity;
    }

    public override string ToString() =>
        $"    {Product.Name} x{Quantity} = {TotalPrice:C}";
}
