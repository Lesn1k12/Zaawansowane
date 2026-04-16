using OrderFlow.Console.Data;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;
using OrderFlow.Console.Services;

System.Console.OutputEncoding = System.Text.Encoding.UTF8;

System.Console.WriteLine("=== TASK 1 — Domain Model & Sample Data ===\n");

System.Console.WriteLine("Products:");
SampleData.Products.ForEach(p => System.Console.WriteLine("  " + p));

System.Console.WriteLine("\nCustomers:");
SampleData.Customers.ForEach(c => System.Console.WriteLine("  " + c));

System.Console.WriteLine("\nOrders:");
foreach (var o in SampleData.Orders)
{
    System.Console.WriteLine("  " + o);
    o.Items.ForEach(i => System.Console.WriteLine(i));
}

System.Console.WriteLine("\n=== TASK 2 — Delegates & Order Validation ===\n");

var validator = new OrderValidator();

var validOrder = SampleData.Orders[0];
System.Console.WriteLine($"Validating (should PASS): {validOrder}");
var (isValid, errors) = validator.ValidateAll(validOrder);
System.Console.WriteLine(isValid ? "  Result: VALID" : "  Result: INVALID");
foreach (var e in errors) System.Console.WriteLine("  - " + e);

var badOrder = new Order(
    SampleData.Customers[0],
    DateTime.Today.AddDays(5),
    OrderStatus.Cancelled,
    new List<OrderItem>()
);
System.Console.WriteLine($"\nValidating (should FAIL multiple rules): {badOrder}");
(isValid, errors) = validator.ValidateAll(badOrder);
System.Console.WriteLine(isValid ? "  Result: VALID" : "  Result: INVALID");
foreach (var e in errors) System.Console.WriteLine("  - " + e);

var processor = new OrderProcessor(SampleData.Orders);
processor.Run();

LinqQueries.Run(SampleData.Orders, SampleData.Customers);

System.Console.WriteLine("\n=== TASK 4 — Persistence (JSON & XML) ===\n");

var repo = new OrderRepository();
int originalCount = SampleData.Orders.Count;
decimal originalTotal = SampleData.Orders.Sum(o => o.TotalAmount);

await repo.SaveToJsonAsync(SampleData.Orders, "data/orders.json");
await repo.SaveToXmlAsync(SampleData.Orders, "data/orders.xml");
System.Console.WriteLine($"Saved {originalCount} orders  (total {originalTotal:C}) to JSON and XML.");

var fromJson = await repo.LoadFromJsonAsync("data/orders.json");
var fromXml  = await repo.LoadFromXmlAsync("data/orders.xml");

System.Console.WriteLine($"JSON loaded: {fromJson.Count} orders, total = {fromJson.Sum(o => o.TotalAmount):C}");
System.Console.WriteLine($"XML  loaded: {fromXml.Count} orders, total = {fromXml.Sum(o => o.TotalAmount):C}");
System.Console.WriteLine(fromJson.Count == originalCount && fromJson.Sum(o => o.TotalAmount) == originalTotal
    ? "JSON: counts and totals match original."
    : "JSON: MISMATCH!");
System.Console.WriteLine(fromXml.Count == originalCount && fromXml.Sum(o => o.TotalAmount) == originalTotal
    ? "XML:  counts and totals match original."
    : "XML:  MISMATCH!");
