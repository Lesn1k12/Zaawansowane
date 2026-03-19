using OrderFlow.Console.Models;

namespace OrderFlow.Console.Data;

public static class SampleData
{
    public static List<Product> Products { get; } = new List<Product>
    {
        new Product("Laptop Pro",     3499.99m, "Electronics"),
        new Product("Wireless Mouse",   49.99m, "Electronics"),
        new Product("Office Chair",    799.00m, "Furniture"),
        new Product("Java Book",        89.99m, "Books"),
        new Product("C# in Depth",      79.99m, "Books"),
        new Product("Standing Desk",  1299.00m, "Furniture"),
        new Product("Headphones",      249.99m, "Electronics"),
    };

    public static List<Customer> Customers { get; } = new List<Customer>
    {
        new Customer("Alice Kowalski",   "Warsaw", isVIP: true),
        new Customer("Bob Nowak",        "Krakow", isVIP: false),
        new Customer("Carol Wisniewski", "Gdansk", isVIP: true),
        new Customer("David Zajac",      "Warsaw", isVIP: false),
    };

    public static List<Order> Orders { get; }

    static SampleData()
    {
        var p = Products;
        var c = Customers;

        Orders = new List<Order>
        {
            new Order(c[0], new DateTime(2026, 1, 10), OrderStatus.Completed,
                new List<OrderItem>
                {
                    new OrderItem(p[0], 1),
                    new OrderItem(p[1], 2),
                }),

            new Order(c[0], new DateTime(2026, 2, 5), OrderStatus.Processing,
                new List<OrderItem>
                {
                    new OrderItem(p[6], 1),
                    new OrderItem(p[3], 1),
                }),

            new Order(c[1], new DateTime(2026, 1, 20), OrderStatus.Validated,
                new List<OrderItem>
                {
                    new OrderItem(p[2], 1),
                    new OrderItem(p[5], 1),
                }),

            new Order(c[2], new DateTime(2026, 3, 1), OrderStatus.New,
                new List<OrderItem>
                {
                    new OrderItem(p[4], 3),
                }),

            new Order(c[3], new DateTime(2026, 2, 14), OrderStatus.Cancelled,
                new List<OrderItem>
                {
                    new OrderItem(p[0], 2),
                }),

            new Order(c[1], new DateTime(2026, 3, 10), OrderStatus.New,
                new List<OrderItem>
                {
                    new OrderItem(p[1], 5),
                    new OrderItem(p[6], 2),
                }),
        };
    }
}
