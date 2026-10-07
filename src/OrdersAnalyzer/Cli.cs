using System.Globalization;
using System.Text.Json;
using OrdersAnalyzer.Models;
using OrdersAnalyzer.Services;

namespace OrdersAnalyzer;

// Takes TextReader/TextWriter instead of Console so tests can feed input and capture output.
public static class Cli
{
    private const string DefaultFileName = "orders.json";
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    public static int Run(string[] args, TextReader input, TextWriter output, TextWriter error)
    {
        var arguments = args.ToList();
        if (!TryExtractFileOption(arguments, out var fileOption))
        {
            error.WriteLine("Missing value for --file.");
            PrintUsage(error);
            return 2;
        }

        var filePath = fileOption ?? ResolveDefaultFile();

        IReadOnlyList<Order> orders;
        try
        {
            orders = OrderRepository.LoadFromFile(filePath);
        }
        catch (FileNotFoundException ex)
        {
            error.WriteLine(ex.Message);
            return 1;
        }
        catch (JsonException ex)
        {
            error.WriteLine($"Invalid JSON in {filePath}: {ex.Message}");
            return 1;
        }

        var service = new OrderService(orders);

        if (arguments.Count == 0)
            return RunInteractive(service, input, output);

        switch (arguments[0].ToLowerInvariant())
        {
            case "list":
                PrintOrders(service.GetAll(), output);
                return 0;

            case "customer" when arguments.Count >= 2:
                // Both  customer "Nino Beridze"  and  customer Nino Beridze  work.
                PrintOrders(service.FindByCustomer(string.Join(' ', arguments.Skip(1))), output);
                return 0;

            case "stats":
                PrintStatistics(service.GetStatistics(), output);
                return 0;

            default:
                PrintUsage(error);
                return 2;
        }
    }

    private static int RunInteractive(OrderService service, TextReader input, TextWriter output)
    {
        while (true)
        {
            output.WriteLine();
            output.WriteLine("1) Show all orders");
            output.WriteLine("2) Find orders by customer");
            output.WriteLine("3) Statistics (completed orders)");
            output.WriteLine("0) Exit");
            output.Write("> ");

            var choice = input.ReadLine();
            if (choice is null) // end of input
                return 0;

            switch (choice.Trim())
            {
                case "1":
                    PrintOrders(service.GetAll(), output);
                    break;
                case "2":
                    output.Write("Customer name: ");
                    PrintOrders(service.FindByCustomer(input.ReadLine() ?? string.Empty), output);
                    break;
                case "3":
                    PrintStatistics(service.GetStatistics(), output);
                    break;
                case "0":
                    return 0;
                default:
                    output.WriteLine("Unknown option.");
                    break;
            }
        }
    }

    public static void PrintOrders(IReadOnlyList<Order> orders, TextWriter output)
    {
        if (orders.Count == 0)
        {
            output.WriteLine("No orders found.");
            return;
        }

        output.WriteLine($"{"ID",-6}{"Customer",-20}{"Status",-11}{"Total",10}");
        output.WriteLine(new string('-', 47));
        foreach (var order in orders)
        {
            output.WriteLine(string.Format(Invariant, "{0,-6}{1,-20}{2,-11}{3,10:0.00}",
                order.Id, order.Customer, order.Status, order.Total));

            foreach (var item in order.Items)
                output.WriteLine(string.Format(Invariant, "        - {0} x{1} @ {2:0.00}{3}",
                    item.Product, item.Quantity, item.Price, item.IsValid ? "" : "  (invalid, ignored)"));
        }
    }

    public static void PrintStatistics(OrderStatistics stats, TextWriter output)
    {
        var average = stats.AverageOrderValue?.ToString("0.00", Invariant) ?? "n/a";
        var top = stats.TopProducts.Count > 0
            ? $"{string.Join(", ", stats.TopProducts)} ({stats.TopQuantity} units)"
            : "n/a";

        output.WriteLine($"Completed orders:     {stats.CompletedCount}");
        output.WriteLine($"Total sales:          {stats.TotalSales.ToString("0.00", Invariant)}");
        output.WriteLine($"Average order value:  {average}");
        output.WriteLine($"Most popular product: {top}");
    }

    private static void PrintUsage(TextWriter output)
    {
        output.WriteLine("Usage:");
        output.WriteLine("  OrdersAnalyzer [--file <path>]                  interactive menu");
        output.WriteLine("  OrdersAnalyzer [--file <path>] list             all orders");
        output.WriteLine("  OrdersAnalyzer [--file <path>] customer <name>  orders of one customer");
        output.WriteLine("  OrdersAnalyzer [--file <path>] stats            completed-order statistics");
    }

    // Removes "--file <path>" from the arguments. Returns false when --file has no value.
    private static bool TryExtractFileOption(List<string> arguments, out string? path)
    {
        path = null;
        var index = arguments.FindIndex(a => a is "--file" or "-f");
        if (index < 0)
            return true;
        if (index == arguments.Count - 1)
            return false;

        path = arguments[index + 1];
        arguments.RemoveRange(index, 2);
        return true;
    }

    // orders.json from the current directory, otherwise the copy next to the executable.
    private static string ResolveDefaultFile() =>
        File.Exists(DefaultFileName)
            ? DefaultFileName
            : Path.Combine(AppContext.BaseDirectory, DefaultFileName);
}
