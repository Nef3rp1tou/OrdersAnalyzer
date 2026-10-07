using OrdersAnalyzer.Models;

namespace OrdersAnalyzer.Services;

public sealed class OrderService(IReadOnlyList<Order> orders)
{
    public IReadOnlyList<Order> GetAll() => orders;

    // Exact name match, ignoring case and extra whitespace ("Nino" does not match "Nino Beridze").
    public IReadOnlyList<Order> FindByCustomer(string name)
    {
        var wanted = NormalizeName(name);
        if (wanted.Length == 0)
            return [];

        return orders
            .Where(o => string.Equals(NormalizeName(o.Customer), wanted, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    // Only completed orders count; cancelled, pending and unknown ones are skipped.
    public OrderStatistics GetStatistics()
    {
        var completed = orders.Where(o => o.Status == OrderStatus.Completed).ToList();

        var totalSales = completed.Sum(o => o.Total);
        decimal? average = completed.Count > 0 ? totalSales / completed.Count : null;

        var unitsByProduct = completed
            .SelectMany(o => o.Items)
            .Where(i => i.IsValid)
            .GroupBy(i => i.Product.Trim(), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Sum(i => i.Quantity));

        var topQuantity = unitsByProduct.Count > 0 ? unitsByProduct.Values.Max() : 0;

        // Every product sharing the max, instead of silently picking one.
        var topProducts = unitsByProduct
            .Where(p => p.Value == topQuantity)
            .Select(p => p.Key)
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new OrderStatistics(
            completed.Count,
            RoundMoney(totalSales),
            average is null ? null : RoundMoney(average.Value),
            topProducts,
            topQuantity);
    }

    // AwayFromZero: 2.345 -> 2.35. The .NET default (banker's rounding) would give 2.34.
    private static decimal RoundMoney(decimal value) =>
        Math.Round(value, 2, MidpointRounding.AwayFromZero);

    private static string NormalizeName(string? name) =>
        string.Join(' ', (name ?? string.Empty).Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
}
