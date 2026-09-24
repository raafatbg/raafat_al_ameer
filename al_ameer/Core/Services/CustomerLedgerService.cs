using al_ameer.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace al_ameer.Services;

public sealed record CustomerBalance(int CustomerId, string FullName, string Phone, decimal TotalInvoiced,
    decimal TotalPaid, decimal Balance);
public sealed record CustomerInvoice(int SaleId, string InvoiceNumber, DateTime SaleDate, decimal GrandTotal,
    decimal PaidAmount, decimal RemainingBalance);
public sealed record CustomerPayment(int CustomerPaymentId, int SaleId, DateTime PaymentDate, decimal Amount,
    string PaymentMethod, string? Notes, DateTime? ReversedAt, string? ReversalReason);

public sealed class CustomerLedgerService
{
    private readonly string _connectionString;
    public CustomerLedgerService(string? connectionString = null) =>
        _connectionString = connectionString ?? DatabaseConfig.ConnectionString;

    public IReadOnlyList<CustomerBalance> GetBalances()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"SELECT c.CustomerId,c.FullName,c.Phone,
            COALESCE(SUM(COALESCE(s.GrandTotal,0)),0),
            COALESCE(SUM(s.PaidAmount),0),
            COALESCE(SUM(s.RemainingBalance),0)
            FROM dbo.Customers c LEFT JOIN dbo.Sales s ON s.CustomerId=c.CustomerId AND s.IsVoided=0
            GROUP BY c.CustomerId,c.FullName,c.Phone ORDER BY c.FullName", connection);
        using var reader = command.ExecuteReader();
        var balances = new List<CustomerBalance>();
        while (reader.Read()) balances.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetString(2),
            reader.GetDecimal(3), reader.GetDecimal(4), reader.GetDecimal(5)));
        return balances;
    }

    public IReadOnlyList<CustomerInvoice> GetInvoices(int customerId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"SELECT SaleId,COALESCE(InvoiceNumber,CONCAT('Sale #',SaleId)),
            SaleDate,COALESCE(GrandTotal,0),PaidAmount,RemainingBalance
            FROM dbo.Sales WHERE CustomerId=@customer AND IsVoided=0 ORDER BY SaleDate DESC,SaleId DESC", connection);
        command.Parameters.Add("@customer", SqlDbType.Int).Value = customerId;
        using var reader = command.ExecuteReader();
        var invoices = new List<CustomerInvoice>();
        while (reader.Read()) invoices.Add(new(reader.GetInt32(0), reader.GetString(1), reader.GetDateTime(2),
            reader.GetDecimal(3), reader.GetDecimal(4), reader.GetDecimal(5)));
        return invoices;
    }

    public IReadOnlyList<CustomerPayment> GetPayments(int customerId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"SELECT CustomerPaymentId,SaleId,PaymentDate,Amount,PaymentMethod,Notes,ReversedAt,ReversalReason FROM
            (SELECT cp.CustomerPaymentId,cp.SaleId,cp.PaymentDate,cp.Amount,cp.PaymentMethod,cp.Notes,cp.ReversedAt,cp.ReversalReason
             FROM dbo.CustomerPayments cp WHERE cp.CustomerId=@customer
             UNION ALL
             SELECT -s.SaleId,s.SaleId,s.SaleDate,
                s.PaidAmount-COALESCE(SUM(CASE WHEN cp.ReversedAt IS NULL THEN cp.Amount ELSE 0 END),0),
                COALESCE(s.PaymentMethod,N'Cash'),CAST(N'Paid at sale' AS nvarchar(400)),
                CAST(NULL AS datetime2(0)),CAST(NULL AS nvarchar(400))
             FROM dbo.Sales s LEFT JOIN dbo.CustomerPayments cp ON cp.SaleId=s.SaleId
             WHERE s.CustomerId=@customer AND s.IsVoided=0
             GROUP BY s.SaleId,s.SaleDate,s.PaidAmount,s.PaymentMethod
             HAVING s.PaidAmount-COALESCE(SUM(CASE WHEN cp.ReversedAt IS NULL THEN cp.Amount ELSE 0 END),0)>0) credits
            ORDER BY PaymentDate DESC,CustomerPaymentId DESC", connection);
        command.Parameters.Add("@customer", SqlDbType.Int).Value = customerId;
        using var reader = command.ExecuteReader();
        var payments = new List<CustomerPayment>();
        while (reader.Read()) payments.Add(new(reader.GetInt32(0), reader.GetInt32(1), reader.GetDateTime(2),
            reader.GetDecimal(3), reader.GetString(4), reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetDateTime(6), reader.IsDBNull(7) ? null : reader.GetString(7)));
        return payments;
    }

    public void RecordPayment(int customerId, int saleId, decimal amount, DateTime paymentDate,
        string method, string? notes)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Payment must be greater than zero.");
        if (string.IsNullOrWhiteSpace(method) || method.Length > 40) throw new ArgumentException("Choose a payment method.");
        if (notes?.Length > 400) throw new ArgumentException("Notes cannot exceed 400 characters.");
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            using var check = new SqlCommand(@"SELECT RemainingBalance,SaleDate FROM dbo.Sales WITH (UPDLOCK,HOLDLOCK)
                WHERE SaleId=@sale AND CustomerId=@customer AND IsVoided=0", connection, transaction);
            check.Parameters.Add("@sale", SqlDbType.Int).Value = saleId;
            check.Parameters.Add("@customer", SqlDbType.Int).Value = customerId;
            decimal remaining;
            DateTime saleDate;
            using (var reader = check.ExecuteReader())
            {
                if (!reader.Read()) throw new InvalidOperationException("Invoice does not belong to this customer.");
                remaining = reader.GetDecimal(0);
                saleDate = reader.GetDateTime(1);
            }
            if (amount > remaining) throw new InvalidOperationException("Payment exceeds the remaining invoice balance.");
            if (paymentDate.Date < saleDate.Date) throw new ArgumentException("Payment date cannot be before the invoice date.");

            using var update = new SqlCommand(@"UPDATE dbo.Sales SET PaidAmount=PaidAmount+@amount,
                RemainingBalance=RemainingBalance-@amount WHERE SaleId=@sale AND CustomerId=@customer
                AND RemainingBalance>=@amount AND IsVoided=0", connection, transaction);
            update.Parameters.Add("@amount", SqlDbType.Decimal).Value = amount;
            update.Parameters.Add("@sale", SqlDbType.Int).Value = saleId;
            update.Parameters.Add("@customer", SqlDbType.Int).Value = customerId;
            if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Invoice balance changed. Refresh and try again.");

            using var insert = new SqlCommand(@"INSERT dbo.CustomerPayments
                (SaleId,CustomerId,Amount,PaymentDate,PaymentMethod,Notes)
                VALUES (@sale,@customer,@amount,@date,@method,@notes)", connection, transaction);
            insert.Parameters.Add("@sale", SqlDbType.Int).Value = saleId;
            insert.Parameters.Add("@customer", SqlDbType.Int).Value = customerId;
            insert.Parameters.Add("@amount", SqlDbType.Decimal).Value = amount;
            insert.Parameters.Add("@date", SqlDbType.DateTime2).Value = paymentDate;
            insert.Parameters.Add("@method", SqlDbType.NVarChar, 40).Value = method.Trim();
            insert.Parameters.Add("@notes", SqlDbType.NVarChar, 400).Value = string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes.Trim();
            insert.ExecuteNonQuery();
            transaction.Commit();
        }
        catch { transaction.Rollback(); throw; }
    }

    public void ReversePayment(int customerId, int paymentId, string reason)
    {
        if (paymentId <= 0) throw new ArgumentException("Only separately recorded payments can be reversed.");
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 400)
            throw new ArgumentException("Enter a reversal reason up to 400 characters.");
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using var select = new SqlCommand(@"SELECT SaleId,Amount,ReversedAt FROM dbo.CustomerPayments WITH (UPDLOCK,HOLDLOCK)
                WHERE CustomerPaymentId=@id AND CustomerId=@customer", connection, transaction);
            select.Parameters.Add("@id", SqlDbType.Int).Value = paymentId;
            select.Parameters.Add("@customer", SqlDbType.Int).Value = customerId;
            int saleId; decimal amount;
            using (var reader = select.ExecuteReader())
            {
                if (!reader.Read()) throw new InvalidOperationException("Payment was not found.");
                if (!reader.IsDBNull(2)) throw new InvalidOperationException("Payment is already reversed.");
                saleId = reader.GetInt32(0); amount = reader.GetDecimal(1);
            }
            using var updateSale = new SqlCommand(@"UPDATE dbo.Sales SET PaidAmount=PaidAmount-@amount,
                RemainingBalance=RemainingBalance+@amount WHERE SaleId=@sale AND CustomerId=@customer AND PaidAmount>=@amount", connection, transaction);
            updateSale.Parameters.Add("@amount", SqlDbType.Decimal).Value = amount;
            updateSale.Parameters.Add("@sale", SqlDbType.Int).Value = saleId;
            updateSale.Parameters.Add("@customer", SqlDbType.Int).Value = customerId;
            if (updateSale.ExecuteNonQuery() != 1) throw new InvalidOperationException("Invoice balance could not be restored.");
            using var updatePayment = new SqlCommand(@"UPDATE dbo.CustomerPayments SET ReversedAt=SYSDATETIME(),
                ReversalReason=@reason WHERE CustomerPaymentId=@id AND ReversedAt IS NULL", connection, transaction);
            updatePayment.Parameters.Add("@reason", SqlDbType.NVarChar, 400).Value = reason.Trim();
            updatePayment.Parameters.Add("@id", SqlDbType.Int).Value = paymentId;
            if (updatePayment.ExecuteNonQuery() != 1) throw new InvalidOperationException("Payment changed. Refresh and retry.");
            transaction.Commit();
        }
        catch { transaction.Rollback(); throw; }
    }
}
