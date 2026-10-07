using OrdersAnalyzer.Services;
using static OrdersAnalyzer.Tests.TestData;

namespace OrdersAnalyzer.Tests;

public class OrderServiceStatisticsTests
{
    private static readonly OrderService Service = new(
    [
        Order(1, "Nino", "completed", ("Laptop", 1, 1000m), ("Mouse", 2, 20m)),
        Order(2, "Giorgi", "cancelled", ("Mouse", 50, 20m)),
        Order(3, "Nino", " COMPLETED ", ("Mouse", 1, 20m)),
        Order(4, "Ana", "pending", ("Laptop", 5, 1000m)),
        Order(5, "Ana", "canceled", ("Laptop", 5, 1000m)),
    ]);

    [Fact]
    public void Statistics_ExcludeCancelledAndPendingOrders()
    {
        var stats = Service.GetStatistics();

        Assert.Equal(2, stats.CompletedCount);    // orders 1 and 3
        Assert.Equal(1060.00m, stats.TotalSales); // 1040 + 20
        Assert.Equal(530.00m, stats.AverageOrderValue);
    }

    [Fact]
    public void MostPopularProduct_IsByUnitsSold_NotByRevenue_AndIgnoresCancelled()
    {
        // Laptop brings more money (1000 vs 60), but Mouse sold more units (3 vs 1).
        // The 50 mice in the cancelled order must not count.
        var stats = Service.GetStatistics();

        Assert.Equal(["Mouse"], stats.TopProducts);
        Assert.Equal(3, stats.TopQuantity);
    }

    [Fact]
    public void MostPopularProduct_ReturnsAllProductsOnTie()
    {
        var service = new OrderService([Order(1, "A", "completed", ("Pen", 2, 1m), ("Book", 2, 5m))]);

        Assert.Equal(["Book", "Pen"], service.GetStatistics().TopProducts);
    }

    [Fact]
    public void MostPopularProduct_SameProductWithDifferentCasingIsCountedTogether()
    {
        var service = new OrderService(
        [
            Order(1, "A", "completed", ("Mouse", 2, 1m), ("Laptop", 3, 1m)),
            Order(2, "B", "completed", ("mouse ", 2, 1m)),
        ]);

        var stats = service.GetStatistics();

        Assert.Equal(["Mouse"], stats.TopProducts);
        Assert.Equal(4, stats.TopQuantity);
    }

    [Fact]
    public void InvalidItems_AreIgnoredInBothSalesAndProductCount()
    {
        // Negative quantity, empty name and negative price: ignored in total sales AND in the product count.
        var service = new OrderService(
        [
            Order(1, "A", "completed", ("Pen", 2, 10m), ("Mouse", -5, 20m), ("", 3, 1m), ("Cup", 1, -4m)),
        ]);

        var stats = service.GetStatistics();

        Assert.Equal(20m, stats.TotalSales);
        Assert.Equal(["Pen"], stats.TopProducts);
    }

    [Fact]
    public void NoCompletedOrders_GivesZeroTotalsAndNoAverage()
    {
        var service = new OrderService([Order(1, "A", "cancelled", ("X", 1, 10m))]);

        var stats = service.GetStatistics();

        Assert.Equal(0, stats.CompletedCount);
        Assert.Equal(0m, stats.TotalSales);
        Assert.Null(stats.AverageOrderValue); // no division by zero
        Assert.Empty(stats.TopProducts);
    }

    [Fact]
    public void Average_IsRoundedToTwoDecimals()
    {
        // 100 / 3 = 33.333...
        var service = new OrderService(
        [
            Order(1, "A", "completed", ("X", 1, 50m)),
            Order(2, "A", "completed", ("X", 1, 25m)),
            Order(3, "A", "completed", ("X", 1, 25m)),
        ]);

        Assert.Equal(33.33m, service.GetStatistics().AverageOrderValue);
    }

    [Fact]
    public void Average_MidpointIsRoundedAwayFromZero_NotBankersRounding()
    {
        // (2.34 + 2.35) / 2 = 2.345 exactly. AwayFromZero gives 2.35; .NET's default would give 2.34.
        var service = new OrderService(
        [
            Order(1, "A", "completed", ("X", 1, 2.34m)),
            Order(2, "A", "completed", ("X", 1, 2.35m)),
        ]);

        Assert.Equal(2.35m, service.GetStatistics().AverageOrderValue);
    }
}
