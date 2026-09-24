using al_ameer.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace al_ameer.Services;

public sealed record StockProduct(int ProductId, string ProductName, string? Barcode, int StockQuantity)
{
    public string DisplayName => $"{ProductName}  •  {Barcode ?? "No barcode"}  •  Stock: {StockQuantity}";
}

public sealed record StockAdjustmentResult(int AdjustmentId, int StockBefore, int StockAfter);

public sealed class InventoryAdjustmentService
{
    private readonly string _connectionString;
    public InventoryAdjustmentService(string? connectionString = null) =>
        _connectionString = connectionString ?? DatabaseConfig.ConnectionString;

    public IReadOnlyList<StockProduct> GetProducts()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"SELECT ProductId,ProductName,Barcode,ISNULL(StockQuantity,0)
            FROM dbo.Products WHERE ISNULL(IsActive,1)=1 ORDER BY ProductName,ProductId", connection);
        using var reader = command.ExecuteReader();
        var products = new List<StockProduct>();
        while (reader.Read()) products.Add(new(reader.GetInt32(0), reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetInt32(3)));
        return products;
    }

    public StockAdjustmentResult Adjust(int productId, int quantityChange, string reason)
    {
        if (productId <= 0 || quantityChange == 0) throw new ArgumentException("Select a product and a nonzero quantity change.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 400)
            throw new ArgumentException("Enter a reason up to 400 characters.");
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using var stock = new SqlCommand(@"SELECT ISNULL(StockQuantity,0) FROM dbo.Products WITH (UPDLOCK,HOLDLOCK)
                WHERE ProductId=@product AND ISNULL(IsActive,1)=1", connection, transaction);
            stock.Parameters.Add("@product", SqlDbType.Int).Value = productId;
            object? current = stock.ExecuteScalar();
            if (current is null) throw new InvalidOperationException("Product no longer exists or is inactive.");
            int before = Convert.ToInt32(current);
            long afterLong = (long)before + quantityChange;
            if (afterLong < 0) throw new InvalidOperationException($"Cannot remove more than {before} items in stock.");
            if (afterLong > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(quantityChange));
            int after = (int)afterLong;
            using var update = new SqlCommand("UPDATE dbo.Products SET StockQuantity=@after WHERE ProductId=@product", connection, transaction);
            update.Parameters.Add("@after", SqlDbType.Int).Value = after;
            update.Parameters.Add("@product", SqlDbType.Int).Value = productId;
            if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Product changed. Refresh and retry.");
            using var audit = new SqlCommand(@"INSERT dbo.InventoryAdjustments
                (ProductId,QuantityChange,StockBefore,StockAfter,Reason)
                OUTPUT INSERTED.AdjustmentId VALUES (@product,@change,@before,@after,@reason)", connection, transaction);
            audit.Parameters.Add("@product", SqlDbType.Int).Value = productId;
            audit.Parameters.Add("@change", SqlDbType.Int).Value = quantityChange;
            audit.Parameters.Add("@before", SqlDbType.Int).Value = before;
            audit.Parameters.Add("@after", SqlDbType.Int).Value = after;
            audit.Parameters.Add("@reason", SqlDbType.NVarChar, 400).Value = reason.Trim();
            int adjustmentId = Convert.ToInt32(audit.ExecuteScalar());
            transaction.Commit();
            return new(adjustmentId, before, after);
        }
        catch { transaction.Rollback(); throw; }
    }
}
