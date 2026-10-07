using OrdersAnalyzer.Services;
using static OrdersAnalyzer.Tests.TestData;

namespace OrdersAnalyzer.Tests;

public class OrderServiceSearchTests
{
    private static readonly OrderService Service = new(
    [
        Order(1, "Nino Beridze", "completed"),
        Order(2, "nino  beridze ", "cancelled"),
        Order(3, "Nino", "completed"),
        Order(4, "Ana Lomidze", "pending"),
    ]);

    [Fact]
    public void FindByCustomer_IgnoresCaseAndExtraSpaces_AndIncludesAllStatuses()
    {
        var found = Service.FindByCustomer("  NINO BERIDZE ");

        Assert.Equal([1, 2], found.Select(o => o.Id));
    }

    [Fact]
    public void FindByCustomer_DoesNotMatchPartialNames()
    {
        Assert.Equal([3], Service.FindByCustomer("Nino").Select(o => o.Id));
    }

    [Theory]
    [InlineData("Unknown Person")]
    [InlineData("")]
    [InlineData("   ")]
    public void FindByCustomer_ReturnsEmptyForUnknownOrBlankName(string name)
    {
        Assert.Empty(Service.FindByCustomer(name));
    }
}
