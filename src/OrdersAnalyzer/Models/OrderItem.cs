namespace OrdersAnalyzer.Models;

public sealed record OrderItem
{
    public string Product { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal Price { get; init; } // decimal, not double: money must be exact

    public decimal Subtotal => Price * Quantity;

    /// <summary>Items without a name, with quantity ≤ 0 or a negative price are ignored everywhere.</summary>
    public bool IsValid => !string.IsNullOrWhiteSpace(Product) && Quantity > 0 && Price >= 0;
}
