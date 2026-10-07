using OrdersAnalyzer.Models;

namespace OrdersAnalyzer.Tests;

internal static class TestData
{
    public static Order Order(int id, string customer, string status, params (string Product, int Quantity, decimal Price)[] items) =>
        new()
        {
            Id = id,
            Customer = customer,
            RawStatus = status,
            Items = items.Select(i => new OrderItem { Product = i.Product, Quantity = i.Quantity, Price = i.Price }).ToList(),
        };
}
