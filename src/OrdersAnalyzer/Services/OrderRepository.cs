using System.Text.Json;
using OrdersAnalyzer.Models;

namespace OrdersAnalyzer.Services;

public static class OrderRepository
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<Order> LoadFromFile(string path)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"File not found: {path}", path);

        return Parse(File.ReadAllText(path));
    }

    public static IReadOnlyList<Order> Parse(string json)
    {
        var orders = JsonSerializer.Deserialize<List<Order?>>(json, Options) ?? [];

        // System.Text.Json ignores nullable annotations: "items": null really produces null.
        return orders
            .OfType<Order>()
            .Select(o => o with { Items = o.Items ?? [], Customer = o.Customer ?? string.Empty })
            .ToList();
    }
}
