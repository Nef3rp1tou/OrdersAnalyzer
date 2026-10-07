using System.Text.Json;
using OrdersAnalyzer.Models;
using OrdersAnalyzer.Services;

namespace OrdersAnalyzer.Tests;

public class OrderRepositoryTests
{
    [Fact]
    public void Parse_ReadsOrdersItemsAndStatus()
    {
        const string json = """
            [
              { "orderId": 7, "customer": "Nino", "status": "Completed",
                "items": [ { "product": "Mouse", "quantity": 3, "price": 0.10 },
                           { "product": "Pad",   "quantity": 1, "price": 0.20 } ] }
            ]
            """;

        var order = Assert.Single(OrderRepository.Parse(json));

        Assert.Equal(7, order.Id);
        Assert.Equal(OrderStatus.Completed, order.Status);
        Assert.Equal(0.50m, order.Total); // exact with decimal; double would give 0.5000000000000001
    }

    [Fact]
    public void Parse_ToleratesNullItems()
    {
        const string json = """[ { "orderId": 1, "customer": "A", "status": "completed", "items": null } ]""";

        var order = Assert.Single(OrderRepository.Parse(json));

        Assert.Empty(order.Items);
        Assert.Equal(0m, order.Total);
    }

    [Theory]
    [InlineData("completed", OrderStatus.Completed)]
    [InlineData("  COMPLETED ", OrderStatus.Completed)]
    [InlineData("Cancelled", OrderStatus.Cancelled)]
    [InlineData("canceled", OrderStatus.Cancelled)]
    [InlineData("pending", OrderStatus.Pending)]
    [InlineData("shipped", OrderStatus.Unknown)]
    [InlineData(null, OrderStatus.Unknown)]
    public void StatusParser_NormalizesVariants(string? raw, OrderStatus expected)
    {
        Assert.Equal(expected, OrderStatusParser.Parse(raw));
    }

    [Theory]
    [InlineData("{ not json")]
    [InlineData("""{ "orderId": 1 }""")] // an object instead of an array
    public void Parse_ThrowsOnInvalidJson(string json)
    {
        Assert.ThrowsAny<JsonException>(() => OrderRepository.Parse(json));
    }
}
