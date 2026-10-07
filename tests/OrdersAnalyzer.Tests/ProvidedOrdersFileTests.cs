using OrdersAnalyzer.Services;

namespace OrdersAnalyzer.Tests;

/// <summary>
/// End-to-end check against the orders.json supplied with the task.
/// Expected values were calculated by hand:
///   completed: #1 (2×50 + 1×25 = 125), #3 (2×25 + 1×200 = 250), #4 (1×50 = 50); #2 is cancelled
///   total = 425, average = 425 / 3 = 141.666… → 141.67
///   units: Keyboard 2 + 1 = 3, Mouse 1 + 2 = 3, Monitor 1 → tie between Keyboard and Mouse
///   (if the cancelled order were counted, Keyboard would win alone with 4)
/// </summary>
public class ProvidedOrdersFileTests
{
    private static readonly OrderService Service =
        new(OrderRepository.LoadFromFile(Path.Combine(AppContext.BaseDirectory, "orders.json")));

    [Fact]
    public void LoadsAllFourOrders()
    {
        Assert.Equal([1, 2, 3, 4], Service.GetAll().Select(o => o.Id));
    }

    [Fact]
    public void Statistics_MatchHandCalculation()
    {
        var stats = Service.GetStatistics();

        Assert.Equal(3, stats.CompletedCount);
        Assert.Equal(425.00m, stats.TotalSales);
        Assert.Equal(141.67m, stats.AverageOrderValue);
        Assert.Equal(["Keyboard", "Mouse"], stats.TopProducts);
        Assert.Equal(3, stats.TopQuantity);
    }

    [Fact]
    public void FindByCustomer_ReturnsBothOfNinosOrders()
    {
        Assert.Equal([1, 3], Service.FindByCustomer("nino").Select(o => o.Id));
    }
}
