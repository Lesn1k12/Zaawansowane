using OrderFlow.Console.Models;

namespace OrderFlow.Console.Services;

public static class LinqQueries
{
    public static void Run(List<Order> orders, List<Customer> customers)
    {
        System.Console.WriteLine("\n=== TASK 4 — LINQ ===\n");

        System.Console.WriteLine("-- Query 1 (Method): Revenue grouped by customer city --");
        var q1 = orders
            .Join(customers,
                  o => o.Customer.Name,
                  c => c.Name,
                  (o, c) => new { City = c.City, Order = o })
            .GroupBy(x => x.City)
            .Select(g => new
            {
                City  = g.Key,
                Count = g.Count(),
                Total = g.Sum(x => x.Order.TotalAmount),
            })
            .OrderByDescending(g => g.Total);

        foreach (var g in q1)
            System.Console.WriteLine($"    {g.City,-10}: {g.Count} orders, total {g.Total:C}");

        System.Console.WriteLine("\n-- Query 2 (Query): Same grouping using query syntax --");
        var q2 =
            from o in orders
            join c in customers on o.Customer.Name equals c.Name
            group o by c.City into g
            orderby g.Sum(o => o.TotalAmount) descending
            select new
            {
                City  = g.Key,
                Count = g.Count(),
                Total = g.Sum(o => o.TotalAmount),
            };

        foreach (var g in q2)
            System.Console.WriteLine($"    {g.City,-10}: {g.Count} orders, total {g.Total:C}");

        System.Console.WriteLine("\n-- Query 3 (Method): Flatten all order lines with SelectMany --");
        var q3 = orders
            .Where(o => o.Status != OrderStatus.Cancelled)
            .SelectMany(o => o.Items, (o, item) => new
            {
                Customer = o.Customer.Name,
                Product  = item.Product.Name,
                Category = item.Product.Category,
                Qty      = item.Quantity,
                Subtotal = item.TotalPrice,
            });

        foreach (var x in q3)
            System.Console.WriteLine($"    {x.Customer,-20} -> {x.Product,-18} ({x.Category}) x{x.Qty} = {x.Subtotal:C}");

        System.Console.WriteLine("\n-- Query 4 (Query): Avg product price per category via SelectMany --");
        var q4 =
            from o in orders
            from item in o.Items
            group item by item.Product.Category into g
            select new
            {
                Category = g.Key,
                AvgPrice = g.Average(i => i.Product.Price),
                TotalQty = g.Sum(i => i.Quantity),
            };

        foreach (var x in q4.OrderBy(x => x.Category))
            System.Console.WriteLine($"    {x.Category,-12}: avg price {x.AvgPrice:C}, total qty sold {x.TotalQty}");

        System.Console.WriteLine("\n-- Query 5 (Method): Top customers by total amount spent --");
        var q5 = orders
            .GroupBy(o => o.Customer)
            .Select(g => new
            {
                Customer  = g.Key.Name,
                IsVIP     = g.Key.IsVIP,
                Orders    = g.Count(),
                Total     = g.Sum(o => o.TotalAmount),
            })
            .OrderByDescending(x => x.Total);

        foreach (var x in q5)
            System.Console.WriteLine($"    {x.Customer,-20}{(x.IsVIP ? " *VIP*" : ""),-7}: {x.Orders} orders, {x.Total:C}");

        System.Console.WriteLine("\n-- Query 6 (Query): GroupJoin — all customers + order summary (left join) --");
        var q6 =
            from c in customers
            join o in orders on c.Name equals o.Customer.Name into customerOrders
            select new
            {
                Customer   = c.Name,
                City       = c.City,
                IsVIP      = c.IsVIP,
                OrderCount = customerOrders.Count(),
                TotalSpent = customerOrders.Sum(o => o.TotalAmount),
            };

        foreach (var x in q6)
            System.Console.WriteLine($"    {x.Customer,-20} [{x.City}]{(x.IsVIP ? " *VIP*" : ""),-7}: {x.OrderCount} orders, {x.TotalSpent:C}");

        System.Console.WriteLine("\n-- Query 7 (Mixed): Per-customer report with favourite category --");
        var q7 = orders
            .GroupBy(o => o.Customer)
            .Select(g =>
            {
                var favCategory =
                    (from o in g
                     from item in o.Items
                     group item by item.Product.Category into catGroup
                     orderby catGroup.Sum(i => i.TotalPrice) descending
                     select catGroup.Key)
                    .FirstOrDefault() ?? "N/A";

                return new
                {
                    Customer    = g.Key.Name,
                    City        = g.Key.City,
                    IsVIP       = g.Key.IsVIP,
                    TotalSpent  = g.Sum(o => o.TotalAmount),
                    FavCategory = favCategory,
                };
            })
            .OrderByDescending(x => x.TotalSpent);

        foreach (var x in q7)
            System.Console.WriteLine(
                $"    {x.Customer,-20} [{x.City}]{(x.IsVIP ? " *VIP*" : ""),-7}: " +
                $"spent {x.TotalSpent,10:C},  fav category: {x.FavCategory}");
    }
}
