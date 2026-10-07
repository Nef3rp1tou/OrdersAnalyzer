namespace OrdersAnalyzer.Models;

// AverageOrderValue is null when there are no completed orders.
// TopProducts holds several names when there is a tie.
public sealed record OrderStatistics(
    int CompletedCount,
    decimal TotalSales,
    decimal? AverageOrderValue,
    IReadOnlyList<string> TopProducts,
    int TopQuantity);
