namespace OrdersAnalyzer.Models;

public enum OrderStatus
{
    Unknown,
    Pending,
    Completed,
    Cancelled,
}

public static class OrderStatusParser
{
    // Tolerates casing, surrounding spaces and the US spelling "canceled".
    public static OrderStatus Parse(string? raw) =>
        raw?.Trim().ToLowerInvariant() switch
        {
            "completed" => OrderStatus.Completed,
            "cancelled" or "canceled" => OrderStatus.Cancelled,
            "pending" => OrderStatus.Pending,
            _ => OrderStatus.Unknown,
        };
}
