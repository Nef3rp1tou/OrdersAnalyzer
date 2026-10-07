using System.Text.Json.Serialization;

namespace OrdersAnalyzer.Models;

public sealed record Order
{
    [JsonPropertyName("orderId")]
    public int Id { get; init; }

    public string Customer { get; init; } = string.Empty;

    [JsonPropertyName("status")]
    public string RawStatus { get; init; } = string.Empty;

    public IReadOnlyList<OrderItem> Items { get; init; } = [];

    [JsonIgnore]
    public OrderStatus Status => OrderStatusParser.Parse(RawStatus);

    [JsonIgnore]
    public decimal Total => Items.Where(i => i.IsValid).Sum(i => i.Subtotal);
}
