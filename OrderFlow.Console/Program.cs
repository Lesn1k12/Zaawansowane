using System.Diagnostics;
using OrderFlow.Console.Data;
using OrderFlow.Console.Events;
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

System.Console.WriteLine("\n=== TASK 5 — Event-Driven Order Pipeline ===\n");

var pipeline = new OrderPipeline();
var stats    = new StatisticsTracker();

pipeline.StatusChanged       += ConsoleLogger.OnStatusChanged;
pipeline.StatusChanged       += EmailNotifier.OnStatusChanged;
pipeline.StatusChanged       += stats.OnStatusChanged;
pipeline.ValidationCompleted += (_, e) =>
{
    var mark = e.IsValid ? "PASS" : "FAIL";
    System.Console.WriteLine($"  [VALID] Order for {e.Order.Customer.Name}: {mark}");
    foreach (var err in e.Errors)
        System.Console.WriteLine($"    - {err}");
};

var pipelineOrders = new List<Order>
{
    new(SampleData.Customers[0], DateTime.Today, OrderStatus.New,
        new List<OrderItem> { new(SampleData.Products[0], 2) }),

    new(SampleData.Customers[1], DateTime.Today, OrderStatus.New,
        new List<OrderItem> { new(SampleData.Products[1], 1), new(SampleData.Products[2], 3) }),

    new(SampleData.Customers[2], DateTime.Today.AddDays(3), OrderStatus.New,
        new List<OrderItem>()),
};

foreach (var order in pipelineOrders)
{
    System.Console.WriteLine($"\nProcessing: {order.Customer.Name}");
    pipeline.ProcessOrder(order);
}

stats.PrintReport();

System.Console.WriteLine("\n=== TASK 6 — Async & Parallel Order Processing ===\n");

var sim        = new ExternalServiceSimulator();
var demoOrders = SampleData.Orders;

System.Console.WriteLine("-- Sequential processing (one at a time) --");
var swSeq = Stopwatch.StartNew();
foreach (var o in demoOrders)
    await sim.ProcessOrderAsync(o);
swSeq.Stop();

System.Console.WriteLine("\n-- Parallel processing (max 3 concurrent) --");
var swPar = Stopwatch.StartNew();
await sim.ProcessMultipleOrdersAsync(demoOrders);
swPar.Stop();

System.Console.WriteLine($"\n-- Timing comparison --");
System.Console.WriteLine($"  Sequential : {swSeq.ElapsedMilliseconds,6} ms");
System.Console.WriteLine($"  Parallel   : {swPar.ElapsedMilliseconds,6} ms");
System.Console.WriteLine($"  Speedup    : {(double)swSeq.ElapsedMilliseconds / swPar.ElapsedMilliseconds:F2}x");

System.Console.WriteLine("\n=== TASK 7 — Thread-Safe Statistics ===");

var baseOrders = SampleData.Orders;
var manyOrders = Enumerable.Range(0, 500)
    .Select(i => baseOrders[i % baseOrders.Count])
    .ToList();

var brokenStats   = new OrderStatistics();
var safeStats     = new OrderStatisticsSafe();
var statValidator = new OrderValidator();

void RecordBroken(Order o)
{
    var (_, errs) = statValidator.ValidateAll(o);
    brokenStats.Record(o, errs);
}

void RecordSafe(Order o)
{
    var (_, errs) = statValidator.ValidateAll(o);
    safeStats.Record(o, errs);
}

System.Console.WriteLine("\n--- PHASE 1: NO synchronization (expect inconsistent counts) ---");
for (int run = 1; run <= 3; run++)
{
    brokenStats.Reset();
    Parallel.ForEach(manyOrders, RecordBroken);
    System.Console.WriteLine($"  Run {run}: TotalProcessed = {brokenStats.TotalProcessed,4}  (expected {manyOrders.Count})");
}

System.Console.WriteLine("\n--- PHASE 2: WITH synchronization (expect consistent counts) ---");
for (int run = 1; run <= 3; run++)
{
    safeStats.Reset();
    Parallel.ForEach(manyOrders, RecordSafe);
    System.Console.WriteLine($"  Run {run}: TotalProcessed = {safeStats.TotalProcessed,4}  (expected {manyOrders.Count})");
}

System.Console.WriteLine("\n-- Safe stats summary (last run) --");
System.Console.WriteLine($"  Total revenue  : {safeStats.TotalRevenue:C}");
System.Console.WriteLine($"  Errors logged  : {safeStats.ProcessingErrors.Count}");
System.Console.WriteLine("  Orders per status:");
foreach (var kv in safeStats.OrdersPerStatus.OrderBy(k => k.Key))
    System.Console.WriteLine($"    {kv.Key,-12}: {kv.Value}");
