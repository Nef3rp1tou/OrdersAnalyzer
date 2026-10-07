# Orders Analyzer

A C# (.NET 10) console application that processes `orders.json`. It can:

1. show all orders;
2. find the orders of a specific customer;
3. skip cancelled orders when calculating statistics;
4. calculate statistics for completed orders: number of completed orders, total sales,
   average order value, and the most popular product by quantity sold.

No third-party libraries are used in the application (`System.Text.Json` for JSON);
tests use xUnit.

## Project structure

```
OrdersAnalyzer.slnx
orders.json                         input data
src/OrdersAnalyzer/
  Program.cs                        entry point, only calls Cli.Run
  Cli.cs                            console commands, interactive menu, output formatting
  Models/
    Order.cs, OrderItem.cs          data model (mirrors the JSON structure)
    OrderStatus.cs                  status enum + normalisation of raw status text
    OrderStatistics.cs              result of the statistics calculation
  Services/
    OrderRepository.cs              reads the file and parses JSON
    OrderService.cs                 business logic: search and statistics
tests/OrdersAnalyzer.Tests/         xUnit tests (33)
```

Responsibilities are separated into three layers:

| Layer | Class | Responsibility | Does NOT |
|---|---|---|---|
| Data access | `OrderRepository` | read and parse `orders.json` | calculate anything |
| Business logic | `OrderService` | search, filtering, statistics | touch files or the console |
| Presentation | `Cli` | parse arguments, menu, printing | contain business rules |

Because `OrderService` works on an in-memory list, the business rules are tested without
files or console I/O, and the data source or the UI could be replaced without touching it.

## How to run

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```bash
dotnet run --project src/OrdersAnalyzer                            # interactive menu
dotnet run --project src/OrdersAnalyzer -- list                    # all orders
dotnet run --project src/OrdersAnalyzer -- customer Nino           # orders of one customer
dotnet run --project src/OrdersAnalyzer -- stats                   # statistics
dotnet run --project src/OrdersAnalyzer -- --file other.json stats # use another file
```

Example output of `stats` on the included `orders.json`:

```
Completed orders:     3
Total sales:          425.00
Average order value:  141.67
Most popular product: Keyboard, Mouse (3 units)
```

Hand calculation for the provided file:

| Order | Customer | Status | Items | Total |
|---|---|---|---|---|
| 1 | Nino | completed | Keyboard 2×50, Mouse 1×25 | 125 |
| 2 | Giorgi | **cancelled** | Keyboard 1×50 | (excluded) |
| 3 | Nino | completed | Mouse 2×25, Monitor 1×200 | 250 |
| 4 | Ana | completed | Keyboard 1×50 | 50 |

- Total: 125 + 250 + 50 = **425.00**; average: 425 / 3 = 141.666… → **141.67**
- Units sold: Keyboard 2 + 1 = **3**, Mouse 1 + 2 = **3**, Monitor 1 → **tie between Keyboard and Mouse**.
  If the cancelled order were (wrongly) included, Keyboard would win alone with 4. So the tie is
  exactly what correct cancelled-order handling produces.

Exit codes: `0` success, `1` file missing or invalid JSON, `2` unknown command / wrong usage.

The solution can also be opened in Visual Studio 2022 (17.13+) or JetBrains Rider.

## Tests

```bash
dotnet test
```

| Test class | What it covers |
|---|---|
| `OrderServiceStatisticsTests` | cancelled/pending excluded, total, average, rounding, most popular product by units (not revenue), rounding mode at the midpoint, ties, case-insensitive product names, invalid items ignored consistently, no completed orders |
| `OrderServiceSearchTests` | search ignores case and extra spaces, no partial matches, blank/unknown names |
| `OrderRepositoryTests` | JSON parsing, `decimal` precision, `items: null`, status variants, invalid JSON |
| `CliTests` | commands, interactive menu, missing file, `--file` without a value, unknown command |
| `ProvidedOrdersFileTests` | end-to-end on the provided `orders.json`: statistics equal the hand calculation above |

## Assumptions and ambiguous cases

- **Which orders count in statistics.** The task says to skip cancelled orders and calculate
  statistics for completed ones, so only `completed` is counted. `pending` and unknown statuses
  are also excluded, because they are not finished sales.
- **Status spelling.** `"Completed"`, `" COMPLETED "` → `Completed`; the US spelling `canceled`
  → `Cancelled`. An unrecognised value (e.g. `shipped`) becomes `Unknown` instead of crashing.
- **Money.** `decimal` everywhere, never `double`: with `double`, `0.1 * 3 + 0.2` ≠ `0.5`.
- **Rounding.** Results are rounded to 2 decimals with `MidpointRounding.AwayFromZero`
  (2.345 → 2.35). .NET's default `Math.Round` uses banker's rounding (2.345 → 2.34), which would
  be surprising in a financial report.
- **Order value.** `Σ quantity × price` over the order's valid items.
- **Invalid items.** An item with an empty product name, a quantity ≤ 0 or a negative price is
  ignored everywhere: in the order total, total sales and the product count (it is still listed,
  marked `(invalid, ignored)`). The provided file has no such items.
- **Average order value.** `total sales / number of completed orders`. With no completed orders
  the average is `null` (printed as `n/a`) instead of dividing by zero.
- **Most popular product.** By total units sold, as the task states ("by quantity sold"), not by
  number of orders or revenue. If several products share the maximum, all of them are shown
  (alphabetically) rather than one picked arbitrarily. This is not hypothetical: the provided file
  produces a Keyboard/Mouse tie. `"Mouse"` and `"mouse "` are the same product.
- **Customer search.** Exact full-name match, ignoring case and extra whitespace. Partial matches
  are deliberately not supported, so searching `"Nino"` would not return a `"Nino Beridze"`.
  Search returns all of the customer's orders, including cancelled ones, because it is a listing,
  not a statistic.
- **Data format.** The file uses `orderId` (mapped to `Order.Id`), `customer`, `status` and
  `items[]` with `product`, `quantity`, `price`. There is no date or currency field.
- **Input file.** The file must be a JSON array, like the provided one. `null` entries and
  `"items": null` do not crash the program. A missing file or invalid JSON prints a clear message
  and returns exit code `1`; `--file` without a path is a usage error (exit code `2`) rather than
  silently falling back to the default file.
- **File location.** `orders.json` is looked up in the current directory first, then next to the
  executable (it is copied to the build output).

## Open questions for the business

The assumptions above are decisions I made to deliver a working result. In a real project I would
confirm them with the stakeholders:

| Question | Current assumption | Why it matters |
|---|---|---|
| Should `pending` orders appear anywhere in the statistics? | Excluded | Useful for revenue forecasting |
| Does "most popular" mean units sold or revenue? | Units sold (as specified) | In the provided data the answer changes: by units it's Keyboard/Mouse (3 each), by revenue it's Monitor (200 vs 150 vs 75) |
| What should be shown when products are tied? | All tied products | The provided data has a tie; a dashboard may have room for only one |
| How is a customer identified? | By normalised name | Both orders of "Nino" are treated as one customer; with no customer ID, two different people named Nino would be merged |
| Are there currencies, discounts, refunds, taxes? | Not in the data, ignored | Total sales could be overstated |
| For which period are statistics needed? | The whole file | Reports are usually per month/quarter |

## Use of AI

- **Tool:** Claude Code (Anthropic Claude).
- **What it helped with:**
  - proposing the project structure (Repository / Service / CLI separation);
  - listing edge cases (status casing, `canceled`/`cancelled`, ties, empty lists, division by
    zero, `items: null`);
  - drafting tests and this README.
- **Choice of language.** The AI's first version was in Python. I decided to redo it in C#,
  because it is my main language and I can verify and explain every line.
- **Errors or wrong logic in AI-suggested code:**
  I did not find errors in the core logic: the AI handled cancelled orders, `decimal` for money and
  division by zero correctly from the start. Before accepting it, I checked the places where this
  kind of task usually goes wrong, and each one is covered by a test: cancelled orders excluded
  from *all* statistics (including the product count), status casing, the rounding mode
  (.NET's default is banker's rounding), and ties for the most popular product, which the provided
  data actually has.

  What I did have to correct:
  - **Over-engineering.** The first version of the file loader also accepted a
    `{"orders": [...]}` wrapper object, JSON comments and trailing commas, which needed a
    two-step parse and an extra helper method. Nothing in the task or the data needs this, so I
    reduced the loader to a single `Deserialize` call.
  - **Too many comments.** Almost every member had an XML doc comment, many of them repeating
    what the name already says. I kept only comments that explain *why* (e.g. `decimal`, rounding
    mode, why `null` items are handled).
  - **Two edge cases found when I asked the AI to critically review its own code:**
    1. `--file` given without a path silently used the default `orders.json`, so the user could
       think they were analysing a different file. Now it is a usage error.
    2. An item with a negative quantity was skipped in the product count but still reduced total
       sales, so the same data was treated differently in two places. Now invalid items are
       ignored consistently everywhere.

    Neither case occurs in the provided file, but both are now covered by tests.
- **How I verified the result:** `dotnet test` (33 tests), recalculating the statistics by hand
  for the provided file (125 + 250 + 50 = 425; 425 / 3 = 141.67; Keyboard 3 = Mouse 3) and locking
  those numbers in `ProvidedOrdersFileTests`, and reading the code line by line.
