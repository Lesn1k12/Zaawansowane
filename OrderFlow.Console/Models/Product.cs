namespace OrderFlow.Console.Models;

public class Product
{
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }

    public Product(string name, decimal price, string category)
    {
        Name = name;
        Price = price;
        Category = category;
    }

    public override string ToString() => $"{Name} ({Category}) - {Price:C}";
}
