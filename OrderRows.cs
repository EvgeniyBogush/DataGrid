namespace EnecaDataGrid;

public sealed class OrderRows
{
    public IReadOnlyList<OrderRow> CreateSample()
    {
        return new[]
        {
            new OrderRow(1001, "Northwind", "Berlin", "Ready", DateTime.Today.AddDays(2), 1420.50m, true),
            new OrderRow(1002, "Contoso", "Seattle", "In review", DateTime.Today.AddDays(4), 850.00m, true),
            new OrderRow(1003, "Fabrikam", "London", "Blocked", DateTime.Today.AddDays(-1), 319.99m, false),
            new OrderRow(1004, "Adventure Works", "Paris", "Ready", DateTime.Today.AddDays(7), 2400.00m, true),
            new OrderRow(1005, string.Empty, "Madrid", "Invalid", DateTime.Today.AddDays(1), 120.00m, false)
        };
    }
}
