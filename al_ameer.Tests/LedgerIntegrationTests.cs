using al_ameer.Data;
using al_ameer.Services;
using Microsoft.Data.SqlClient;

namespace al_ameer.Tests;

[Collection("database")]
public class LedgerIntegrationTests
{
    [Fact]
    public void FinancialReportsLoadFromCurrentSchema()
    {
        var reports = new FinancialReportService();
        Assert.NotNull(reports.GetCashFlow(DateTime.Today.AddDays(-30), DateTime.Today));
        Assert.NotNull(reports.GetCustomerAging(DateTime.Today));
        Assert.NotNull(reports.GetSalaryBalances());
    }

    [Fact]
    public void CustomerPaymentUpdatesInvoiceAndRejectsOverpayment()
    {
        int customerId = CreateCustomer();
        int productId = CreateProduct();
        int? saleId = null;
        try
        {
            var sale = new SaleService().Complete(new(customerId,
                [new(productId, "Ledger test", 100m, 1)], 0m, 0m, "Credit", 0m));
            saleId = sale.SaleId;
            var ledger = new CustomerLedgerService();
            Assert.Equal(100m, ledger.GetInvoices(customerId).Single().RemainingBalance);
            ledger.RecordPayment(customerId, sale.SaleId, 40m, DateTime.Today, "Cash", "Test payment");
            Assert.Equal(60m, ledger.GetInvoices(customerId).Single().RemainingBalance);
            Assert.Single(ledger.GetPayments(customerId));
            Assert.Throws<InvalidOperationException>(() => ledger.RecordPayment(customerId, sale.SaleId, 61m,
                DateTime.Today, "Cash", null));
            Assert.Equal(60m, ledger.GetInvoices(customerId).Single().RemainingBalance);
            Assert.Single(ledger.GetPayments(customerId));
            int paymentId = ledger.GetPayments(customerId).Single().CustomerPaymentId;
            ledger.ReversePayment(customerId, paymentId, "Test correction");
            Assert.Equal(100m, ledger.GetInvoices(customerId).Single().RemainingBalance);
            Assert.NotNull(ledger.GetPayments(customerId).Single().ReversedAt);
            Assert.Throws<InvalidOperationException>(() => ledger.ReversePayment(customerId, paymentId, "Again"));
        }
        finally
        {
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();
            if (saleId is int id)
            {
                using var deletePayments = new SqlCommand("DELETE FROM CustomerPayments WHERE SaleId=@id", connection);
                deletePayments.Parameters.AddWithValue("@id", id);
                deletePayments.ExecuteNonQuery();
                new SaleService().Void(id);
                DeleteById(connection, "SaleTenderReceipts", "SaleId", id);
                DeleteById(connection, "SaleItems", "SaleId", id);
                DeleteById(connection, "Sales", "SaleId", id);
            }
            DeleteById(connection, "Products", "ProductId", productId);
            DeleteById(connection, "Customers", "CustomerId", customerId);
        }
    }

    [Fact]
    public void SalaryLedgerCalculatesBalanceAndRejectsExcessPayment()
    {
        var ledger = new EmployeeLedgerService();
        int employeeId = ledger.SaveEmployee(null, "__codex_test_employee", "000", "Technician",
            DateTime.Today, 1_000m, true);
        try
        {
            ledger.AddEntry(employeeId, DateTime.Today, "Due", 1_000m, "Test month");
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();
            Assert.Equal(0, Scalar<int>(connection,
                "SELECT COUNT(*) FROM Expenses x JOIN EmployeeSalaryEntries s ON s.SalaryEntryId=x.SalaryEntryId WHERE s.EmployeeId=@id", employeeId));
            ledger.AddEntry(employeeId, DateTime.Today, "Payment", 400m, "Test payment");
            Assert.Equal(1, Scalar<int>(connection,
                "SELECT COUNT(*) FROM Expenses x JOIN EmployeeSalaryEntries s ON s.SalaryEntryId=x.SalaryEntryId WHERE s.EmployeeId=@id AND x.Category='Salary' AND x.Amount=400 AND x.ReversedAt IS NULL", employeeId));
            Assert.Equal(600m, ledger.GetEmployees().Single(e => e.EmployeeId == employeeId).SalaryBalance);
            Assert.Throws<InvalidOperationException>(() => ledger.AddEntry(employeeId, DateTime.Today, "Payment", 601m, null));
            Assert.Equal(2, ledger.GetEntries(employeeId).Count);
            var entries = ledger.GetEntries(employeeId);
            int dueId = entries.Single(e => e.EntryType == "Due").SalaryEntryId;
            int paymentId = entries.Single(e => e.EntryType == "Payment").SalaryEntryId;
            Assert.Throws<InvalidOperationException>(() => ledger.ReverseEntry(employeeId, dueId, "Premature"));
            ledger.ReverseEntry(employeeId, paymentId, "Test correction");
            Assert.Equal(1, Scalar<int>(connection,
                "SELECT COUNT(*) FROM Expenses WHERE SalaryEntryId=@id AND ReversedAt IS NOT NULL AND ReversalReason='Test correction'", paymentId));
            Assert.Equal(1_000m, ledger.GetEmployees().Single(e => e.EmployeeId == employeeId).SalaryBalance);
            ledger.ReverseEntry(employeeId, dueId, "Test correction");
            Assert.Equal(0m, ledger.GetEmployees().Single(e => e.EmployeeId == employeeId).SalaryBalance);
        }
        finally
        {
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();
            using (var deleteExpenses = new SqlCommand(@"DELETE x FROM Expenses x JOIN EmployeeSalaryEntries s
                ON s.SalaryEntryId=x.SalaryEntryId WHERE s.EmployeeId=@id", connection))
            {
                deleteExpenses.Parameters.AddWithValue("@id", employeeId);
                deleteExpenses.ExecuteNonQuery();
            }
            DeleteById(connection, "EmployeeSalaryEntries", "EmployeeId", employeeId);
            DeleteById(connection, "Employees", "EmployeeId", employeeId);
        }
    }

    [Fact]
    public void InventoryAdjustmentsKeepStockAndReasonHistoryTogether()
    {
        int productId = CreateProduct();
        var service = new InventoryAdjustmentService();
        try
        {
            Assert.Contains(service.GetProducts(), p => p.ProductId == productId);
            var added = service.Adjust(productId, 3, "Test delivery");
            Assert.Equal(1, added.StockBefore);
            Assert.Equal(4, added.StockAfter);
            var removed = service.Adjust(productId, -2, "Test damage");
            Assert.Equal(4, removed.StockBefore);
            Assert.Equal(2, removed.StockAfter);
            Assert.Throws<InvalidOperationException>(() => service.Adjust(productId, -3, "Too many"));
            Assert.Throws<ArgumentException>(() => service.Adjust(productId, 1, " "));
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();
            Assert.Equal(2, Scalar<int>(connection, "SELECT StockQuantity FROM Products WHERE ProductId=@id", productId));
            Assert.Equal(2, Scalar<int>(connection, "SELECT COUNT(*) FROM InventoryAdjustments WHERE ProductId=@id", productId));
            Assert.Equal(1, Scalar<int>(connection,
                "SELECT COUNT(*) FROM InventoryAdjustments WHERE ProductId=@id AND QuantityChange=-2 AND Reason='Test damage' AND StockBefore=4 AND StockAfter=2", productId));
        }
        finally
        {
            using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
            connection.Open();
            DeleteById(connection, "InventoryAdjustments", "ProductId", productId);
            DeleteById(connection, "Products", "ProductId", productId);
        }
    }

    private static int CreateCustomer()
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        connection.Open();
        using var command = new SqlCommand("INSERT Customers (FullName,Phone) OUTPUT INSERTED.CustomerId VALUES (@name,'000')", connection);
        command.Parameters.AddWithValue("@name", "__codex_test_customer_" + Guid.NewGuid().ToString("N"));
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static int CreateProduct()
    {
        using var connection = new SqlConnection(DatabaseConfig.ConnectionString);
        connection.Open();
        using var command = new SqlCommand(@"INSERT Products (ProductName,CostPrice,SellingPrice,StockQuantity,IsActive,Currency,CostPriceUSD,SellingPriceUSD)
            OUTPUT INSERTED.ProductId VALUES ('__codex_test_product',50,100,1,1,'LBP',0,0)", connection);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    private static void DeleteById(SqlConnection connection, string table, string key, int id)
    {
        // Table and key are fixed literals used only in this test class.
        using var command = new SqlCommand($"DELETE FROM {table} WHERE {key}=@id", connection);
        command.Parameters.AddWithValue("@id", id);
        command.ExecuteNonQuery();
    }

    private static T Scalar<T>(SqlConnection connection, string sql, int id)
    {
        using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@id", id);
        return (T)Convert.ChangeType(command.ExecuteScalar()!, typeof(T));
    }
}
