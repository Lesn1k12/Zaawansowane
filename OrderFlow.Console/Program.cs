using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using OrderFlow.Console.Data;
using OrderFlow.Console.Events;
using OrderFlow.Console.Models;
using OrderFlow.Console.Persistence;
using OrderFlow.Console.Reports;
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

System.Console.WriteLine("\n=== TASK 8 — LINQ to XML Report ===\n");

var reportBuilder = new XmlReportBuilder();
var report        = reportBuilder.BuildReport(SampleData.Orders);

await reportBuilder.SaveReportAsync(report, "data/report.xml");
System.Console.WriteLine("Report saved to data/report.xml");
System.Console.WriteLine(report.ToString());

var highValueIds = await reportBuilder.FindHighValueOrderIdsAsync("data/report.xml", 1000m);
System.Console.WriteLine($"\nOrder IDs with total > 1000: {string.Join(", ", highValueIds)}");

System.Console.WriteLine("\n=== TASK 9 — Inbox Watcher ===\n");

var inboxPipeline = new OrderPipeline();
inboxPipeline.StatusChanged += ConsoleLogger.OnStatusChanged;
inboxPipeline.StatusChanged += EmailNotifier.OnStatusChanged;

using var watcher = new InboxWatcher("inbox", inboxPipeline);
System.Console.WriteLine("Watcher started on inbox/. Dropping 3 test files...\n");

var inboxRepo = new OrderRepository();
for (int wave = 1; wave <= 3; wave++)
{
    await Task.Delay(3000);
    var waveOrders = new List<Order>
    {
        new(SampleData.Customers[wave % SampleData.Customers.Count],
            DateTime.Today, OrderStatus.New,
            new List<OrderItem> { new(SampleData.Products[wave % SampleData.Products.Count], wave) }),

        new(SampleData.Customers[(wave + 1) % SampleData.Customers.Count],
            DateTime.Today, OrderStatus.New,
            new List<OrderItem> { new(SampleData.Products[(wave + 2) % SampleData.Products.Count], 1) }),
    };
    var inboxFile = Path.Combine("inbox", $"wave{wave}.json");
    await inboxRepo.SaveToJsonAsync(waveOrders, inboxFile);
    System.Console.WriteLine($"[DEMO] Dropped {inboxFile} ({waveOrders.Count} orders)");
}

await Task.Delay(2000);
System.Console.WriteLine("\nWatcher demo complete.");

// ═══════════════════════════════════════════════════════════════════════════
System.Console.WriteLine("\n=== TASK 10 — Entity Framework Core 10 + SQLite ===\n");

await using var db = new OrderFlowContext();

// ── Migracje + seeding ────────────────────────────────────────────────────
System.Console.WriteLine("[EF] Stosowanie migracji...");
await db.Database.MigrateAsync();

System.Console.WriteLine("[EF] Seeding bazy danych...");
await DatabaseSeeder.SeedAsync(db);

// ── CRUD ──────────────────────────────────────────────────────────────────
System.Console.WriteLine("\n--- CRUD ---");

// CREATE: nowe zamówienie z 2 pozycjami
var firstCustId  = await db.Customers.Select(c => c.Id).FirstAsync();
var productIds   = await db.Products.Select(p => p.Id).Take(2).ToListAsync();
var createdOrder = await EfOrderService.CreateOrderAsync(
    db, firstCustId, (productIds[0], 1), (productIds[1], 3));

// UPDATE: zmiana statusu + notatka
await EfOrderService.UpdateOrderAsync(db, createdOrder.Id, "Zamówienie pilnie przetwarzane");

// DELETE: usuń wszystkie Cancelled
await EfOrderService.DeleteCancelledOrdersAsync(db);

// ── Transakcja ────────────────────────────────────────────────────────────
System.Console.WriteLine("\n--- Transakcja: ProcessOrderAsync ---");

// Scenariusz A — sukces (zamówienie ze statusem New/Validated + jest stock)
var orderToProcess = await db.Orders
    .Include(o => o.Items).ThenInclude(oi => oi.Product)
    .FirstOrDefaultAsync(o => o.Status == OrderStatus.New
                           || o.Status == OrderStatus.Validated);

if (orderToProcess != null)
{
    System.Console.WriteLine($"[TX] Przetwarzam Order Id={orderToProcess.Id}...");
    try { await EfOrderService.ProcessOrderAsync(db, orderToProcess.Id); }
    catch (Exception ex) { System.Console.WriteLine($"[TX] Błąd: {ex.Message}"); }
}

// Scenariusz B — rollback (zerujemy stock, by wymusić wyjątek)
var demoOrder = await db.Orders
    .Include(o => o.Items).ThenInclude(oi => oi.Product)
    .FirstOrDefaultAsync(o => o.Status == OrderStatus.New);

if (demoOrder != null)
{
    System.Console.WriteLine("\n[TX] Demonstracja rollbacku — zerujemy stock...");
    demoOrder.Items.First().Product.Stock = 0;
    await db.SaveChangesAsync();

    try { await EfOrderService.ProcessOrderAsync(db, demoOrder.Id); }
    catch (InvalidOperationException ex)
    {
        System.Console.WriteLine($"[TX] Rollback zakończony poprawnie. Powód: {ex.Message}");
    }
}

// ── Zaawansowane zapytania LINQ ───────────────────────────────────────────
System.Console.WriteLine("\n--- Zaawansowane zapytania LINQ (IQueryable) ---");

await EfLinqQueries.PrintVipOrdersAbove500Async(db);
await EfLinqQueries.PrintTop3CustomersBySpendingAsync(db);
await EfLinqQueries.PrintAvgOrderValuePerCityAsync(db);
await EfLinqQueries.PrintNeverOrderedProductsAsync(db);

// Q5a — filtr: Completed, dowolna kwota
await EfLinqQueries.PrintDynamicFilterAsync(db, OrderStatus.Completed, minAmount: null);
// Q5b — filtr: dowolny status, minimum 100 PLN
await EfLinqQueries.PrintDynamicFilterAsync(db, status: null, minAmount: 100m);

System.Console.WriteLine("\n=== EF Core demo zakończone. ===");

// ═══════════════════════════════════════════════════════════════════════════
System.Console.WriteLine("\n=== LAB 5 — Currency Conversion (NBP API) ===\n");

var httpClient        = new HttpClient();
var currencyService   = new CurrencyService(httpClient);
var currencyConverter = new OrderCurrencyConverter(currencyService);

var ordersForConversion = await db.Orders
    .Include(o => o.Customer)
    .Include(o => o.Items).ThenInclude(oi => oi.Product)
    .Take(5)
    .ToListAsync();

System.Console.WriteLine($"{"Customer",-20} {"PLN",12} {"USD",12} {"EUR",12}");
System.Console.WriteLine(new string('-', 60));

foreach (var order in ordersForConversion)
{
    var usd = await currencyConverter.ConvertOrderTotalAsync(order, "USD");
    var eur = await currencyConverter.ConvertOrderTotalAsync(order, "EUR");

    System.Console.WriteLine(
        $"{order.Customer.Name,-20} {order.TotalAmount,12:F2} " +
        $"{(usd.HasValue ? usd.Value.ToString("F2") : "N/A"),12} " +
        $"{(eur.HasValue ? eur.Value.ToString("F2") : "N/A"),12}");
}

System.Console.WriteLine("\n=== Currency conversion zakończone. ===");
