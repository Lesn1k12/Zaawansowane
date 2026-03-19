using OrderFlow.Console.Data;
using OrderFlow.Console.Models;
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
