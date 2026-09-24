using al_ameer.Data;
using al_ameer.Services;
using Microsoft.Data.SqlClient;

namespace al_ameer.Tests;

[CollectionDefinition("database", DisableParallelization = true)]
public sealed class DatabaseCollection : ICollectionFixture<object> { }

[Collection("database")]
public class SaleIntegrationTests
{
    [Fact]
    public void MixedProductAndServiceSaleKeepsServiceOutOfInventory()
    {
        int productId = CreateProduct(3);
        int serviceId = new ServicesCatalogService().Save(null,
            "__codex_service_" + Guid.NewGuid().ToString("N"), null, 150m, true);
        SaleResult? sale = null;
        try
        {
            var browser = new SaleBrowserService().Load();
            Assert.Contains(browser, folder => folder.Name == "Services" &&
                folder.Children.Any(item => item.Id == serviceId && item.Kind == "Service"));
            sale = new SaleService().Complete(new(null,
                [new(productId, "Test product", 100m, 1), new(0, "Test service", 150m, 1, serviceId)],
                0m, 0m, "Cash", 250m));
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();
            Assert.Equal(2, Scalar<int>(connection, "SELECT COUNT(*) FROM SaleItems WHERE SaleId=@id", sale.SaleId));
            Assert.Equal(1, Scalar<int>(connection, "SELECT COUNT(*) FROM SaleItems WHERE SaleId=@id AND ItemType='Service' AND ProductId IS NULL AND ServiceId IS NOT NULL", sale.SaleId));
            Assert.Equal(2, Scalar<int>(connection, "SELECT StockQuantity FROM Products WHERE ProductId=@id", productId));
            new SaleService().Void(sale.SaleId);
            Assert.Equal(1, Scalar<int>(connection, "SELECT COUNT(*) FROM SaleTenderReceipts WHERE SaleId=@id", sale.SaleId));
            Assert.Equal(1, Scalar<int>(connection, "SELECT COUNT(*) FROM Sales WHERE SaleId=@id AND IsVoided=1", sale.SaleId));
            Assert.Equal(3, Scalar<int>(connection, "SELECT StockQuantity FROM Products WHERE ProductId=@id", productId));
        }
        finally
        {
            if (sale is not null) DeleteSaleForTest(sale.SaleId);
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();
            using var delete = new SqlCommand("DELETE FROM ServicesCatalog WHERE ServiceID=@id", connection);
            delete.Parameters.AddWithValue("@id", serviceId);
            delete.ExecuteNonQuery();
            DeleteProduct(productId);
        }
    }

    [Fact]
    public void SaleCompletesPersistsPaymentAndVoidRestoresInventory()
    {
        int productId = CreateProduct(5);
        int customerId = CreateCustomer();
        SaleResult? sale = null;
        try
        {
            sale = new SaleService().Complete(new(customerId,
                [new(productId, "Automated test product", 100m, 2)], 10m, 10m, "Credit", 100m));
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();
            Assert.Equal(3, Scalar<int>(connection, "SELECT StockQuantity FROM Products WHERE ProductId=@id", productId));
            Assert.Equal(109m, Scalar<decimal>(connection, "SELECT RemainingBalance FROM Sales WHERE SaleId=@id", sale.SaleId));
            Assert.Equal(100m, new CustomerLedgerService().GetPayments(customerId).Single().Amount);
            new SaleService().Void(sale.SaleId);
            Assert.Equal(1, Scalar<int>(connection, "SELECT COUNT(*) FROM SaleTenderReceipts WHERE SaleId=@id", sale.SaleId));
            Assert.Equal(5, Scalar<int>(connection, "SELECT StockQuantity FROM Products WHERE ProductId=@id", productId));
        }
        finally
        {
            if (sale != null) DeleteSaleForTest(sale.SaleId);
            DeleteProduct(productId);
            DeleteCustomer(customerId);
        }
    }

    [Fact]
    public void InsufficientStockRollsBackSaleAndInventory()
    {
        int productId = CreateProduct(1);
        try
        {
            Assert.Throws<InsufficientStockException>(() => new SaleService().Complete(new(null,
                [new(productId, "Automated rollback product", 100m, 2)], 0m, 0m, "Cash", 200m)));
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();
            Assert.Equal(1, Scalar<int>(connection, "SELECT StockQuantity FROM Products WHERE ProductId=@id", productId));
            Assert.Equal(0, Scalar<int>(connection, "SELECT COUNT(*) FROM SaleItems WHERE ProductId=@id", productId));
        }
        finally { DeleteProduct(productId); }
    }

    private static int CreateProduct(int stock)
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        connection.Open();
        using var command = new SqlCommand(@"INSERT INTO Products
            (ProductName, CostPrice, SellingPrice, StockQuantity, IsTire, IsActive, Currency, CostPriceUSD, SellingPriceUSD)
            OUTPUT INSERTED.ProductId VALUES (@name, 50, 100, @stock, 0, 1, 'LBP', 0, 0)", connection);
        command.Parameters.AddWithValue("@name", "__codex_test_" + Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("@stock", stock);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static int CreateCustomer()
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        connection.Open();
        using var command = new SqlCommand("INSERT Customers (FullName,Phone) OUTPUT INSERTED.CustomerId VALUES (@name,'000')", connection);
        command.Parameters.AddWithValue("@name", "__codex_test_" + Guid.NewGuid().ToString("N"));
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void DeleteCustomer(int id)
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        connection.Open();
        using var command = new SqlCommand("DELETE FROM Customers WHERE CustomerId=@id", connection);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    private static void DeleteSaleForTest(int saleId)
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        connection.Open();
        using var command = new SqlCommand(@"DELETE FROM SaleTenderReceipts WHERE SaleId=@id;
            DELETE FROM SaleItems WHERE SaleId=@id;
            DELETE FROM Sales WHERE SaleId=@id;", connection);
        command.Parameters.AddWithValue("@id", saleId);
        command.ExecuteNonQuery();
    }

    private static T Scalar<T>(SqlConnection connection, string sql, int id)
    {
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }

    private static void DeleteProduct(int id)
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        connection.Open();
        using var command = new SqlCommand("DELETE FROM Products WHERE ProductId=@id", connection);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }
}
