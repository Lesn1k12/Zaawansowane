namespace OrderFlow.Console.Models;

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Category { get; set; } = string.Empty;
    public int Stock { get; set; }

    public Product() { }

    public Product(string name, decimal price, string category, int stock = 0)
    {
        Name = name;
        Price = price;
        Category = category;
        Stock = stock;
    }

    public override string ToString() => $"{Name} ({Category}) - {Price:C} [Stock: {Stock}]";
}
