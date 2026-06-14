# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Commands

All commands run from `OrderFlow/` (the solution root).

```bash
# Build
dotnet build OrderFlow.sln

# Run the console app (from OrderFlow.Console/)
dotnet run --project OrderFlow.Console

# Run all tests
dotnet test OrderFlow.sln

# Run a single test class
dotnet test OrderFlow.Tests --filter "FullyQualifiedName~OrderProcessorTests"

# Run a single test method
dotnet test OrderFlow.Tests --filter "FullyQualifiedName~OrderProcessorTests.FilterOrders_CompletedPredicate_ReturnsOnlyCompletedOrders"

# Apply EF Core migrations (from OrderFlow.Console/)
dotnet ef database update --project OrderFlow.Console

# Add a new EF Core migration
dotnet ef migrations add <MigrationName> --project OrderFlow.Console
```

## Architecture

The solution has two projects:

- **OrderFlow.Console** — main executable (net10.0), depends on EF Core + SQLite
- **OrderFlow.Tests** — xUnit test suite with Moq, references OrderFlow.Console

### Domain model (`Models/`)

`Product`, `Customer`, `Order`, `OrderItem`, `OrderStatus` (enum). Key notes:
- `Customer.Name` is a `[NotMapped]` alias for `FullName` (EF column). Both must stay in sync when customer data is created.
- `Order.TotalAmount` and `OrderItem.TotalPrice` are computed properties — EF ignores them via `Ignore()` in `OrderFlowContext.OnModelCreating`.
- `Customer` has a navigational `ICollection<Order> Orders` used by EF; in-memory code uses `SampleData` lists directly.

### Services (`Services/`)

| Class | Responsibility |
|---|---|
| `OrderValidator` | Validation pipeline: custom `ValidationRule` delegate + `Func<Order,bool>` lambdas |
| `OrderProcessor` | `Predicate<Order>`, `Action<Order>`, `Func<Order,T>` patterns + aggregation |
| `LinqQueries` | In-memory LINQ demos (both method and query syntax) |
| `EfLinqQueries` | `IQueryable`-based EF LINQ queries (VIP orders, top customers, avg per city, etc.) |
| `EfOrderService` | EF CRUD + transactional `ProcessOrderAsync` (checks stock, transitions status) |
| `CurrencyService` | Fetches exchange rates from NBP API with an in-memory cache; interface `ICurrencyService` for testability |
| `OrderCurrencyConverter` | Uses `ICurrencyService` to convert order totals to a target currency |
| `OrderPipeline` | Event-driven pipeline: fires `StatusChanged` and `ValidationCompleted` events as orders move through statuses |
| `OrderPipelineSubscribers` | `ConsoleLogger`, `EmailNotifier`, `StatisticsTracker` — event handlers wired in `Program.cs` |
| `ExternalServiceSimulator` | Async/parallel processing demo with configurable delays |
| `OrderStatistics` / `OrderStatisticsSafe` | Demonstrate unsafe vs. thread-safe stats accumulation under `Parallel.ForEach` |
| `InboxWatcher` | `FileSystemWatcher` on an `inbox/` directory; picks up JSON order files dropped there |

### Persistence (`Persistence/`)

- `OrderFlowContext` — EF Core DbContext targeting `orderflow.db` (SQLite). Migrations live in `Migrations/`.
- `DatabaseSeeder` — idempotent seed: checks row counts before inserting.
- `OrderRepository` — async JSON (System.Text.Json) and XML (LINQ to XML) save/load helpers; files go to `data/`.

### Reports (`Reports/`)

`XmlReportBuilder` — builds an `XDocument` summary of orders and saves/queries it asynchronously.

### Test strategy

Tests use xUnit + Moq. `ICurrencyService` is mocked via `TestHttpMessageHandler` for `CurrencyServiceTests`. Domain helpers (`MakeOrder`) are defined as `static` factory methods inside each test class to keep tests self-contained.

### EF Core specifics

- SQLite file: `orderflow.db` created at the working directory of the console app.
- `DeleteBehavior.Cascade` on `Order → OrderItems`; `Restrict` on `Customer → Orders` and `OrderItem → Product`.
- Decimal precision is pinned to `(18,2)` for `Product.Price`.
- `Customer` is indexed on `FullName`; `Order` is indexed on `Status`.
