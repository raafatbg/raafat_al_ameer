using al_ameer.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace al_ameer.Services;

public sealed record SaleLine(int ProductId, string ProductName, decimal UnitPrice, int Quantity, int? ServiceId = null);
public sealed record SaleRequest(int? CustomerId, IReadOnlyList<SaleLine> Lines, decimal Discount,
    decimal TaxPercent, string PaymentMethod, decimal PaidAmount, TenderInput? Tender = null);
public sealed record SaleResult(int SaleId, string InvoiceNumber, BillingTotals Totals, decimal PaidAmount,
    decimal RemainingBalance, string ReceiptNumber, TenderSummary Tender);

public sealed class InsufficientStockException(string productName)
    : InvalidOperationException($"Insufficient stock for {productName}.");

public sealed class SaleService
{
    private readonly string _connectionString;
    public SaleService(string? connectionString = null) => _connectionString = connectionString ?? DatabaseConfig.ConnectionString;

    public SaleResult Complete(SaleRequest request)
    {
        if (request.Lines.Count == 0) throw new ArgumentException("A sale must contain at least one item.");
        if (request.Lines.Any(x => x.Quantity <= 0 || x.UnitPrice < 0 ||
            (x.ServiceId is null && x.ProductId <= 0) || (x.ServiceId is not null && (x.ProductId != 0 || x.ServiceId <= 0))))
            throw new ArgumentException("Sale contains an invalid product or service line.");
        BillingTotals totals = BillingCalculator.Calculate(request.Lines.Select(x => (x.UnitPrice, x.Quantity)), request.Discount, request.TaxPercent);
        TenderSummary tender = TenderCalculator.Calculate(totals.GrandTotal,
            request.Tender ?? new TenderInput(request.PaidAmount, 0m, AppSettings.Current.LbpPerUsd));
        if (request.PaidAmount != tender.AppliedLBP)
            throw new ArgumentException("Paid amount does not match received LBP and USD tender.");
        if (tender.RemainingLBP > 0 && request.CustomerId is null)
            throw new ArgumentException("Select a customer before recording an unpaid balance.");
        string method = string.IsNullOrWhiteSpace(request.PaymentMethod) ? "Cash" : request.PaymentMethod.Trim();
        if (method == "Card" && tender.ChangeLBP > 0)
            throw new ArgumentException("Card tender cannot exceed the sale total.");
        if (method == "Cash" && tender.RemainingLBP > 0)
            throw new ArgumentException("For a partial payment, choose Credit and select a customer.");
        string invoice = $"INV-{DateTime.Now:yyMMdd-HHmmssfff}";

        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
        try
        {
            using var sale = new SqlCommand(@"INSERT INTO Sales
                (InvoiceNumber, CustomerId, SaleDate, SubTotal, TaxAmount, GrandTotal, PaymentMethod, Notes, PaidAmount, RemainingBalance)
                OUTPUT INSERTED.SaleId
                VALUES (@invoice, @customer, GETDATE(), @subtotal, @tax, @grand, @method, @notes, @paid, @remaining)", connection, transaction);
            decimal remaining = tender.RemainingLBP;
            sale.Parameters.Add("@invoice", SqlDbType.NVarChar, 40).Value = invoice;
            sale.Parameters.Add("@customer", SqlDbType.Int).Value = request.CustomerId is null ? DBNull.Value : request.CustomerId.Value;
            sale.Parameters.Add("@subtotal", SqlDbType.Decimal).Value = totals.Subtotal;
            sale.Parameters.Add("@tax", SqlDbType.Decimal).Value = totals.TaxAmount;
            sale.Parameters.Add("@grand", SqlDbType.Decimal).Value = totals.GrandTotal;
            sale.Parameters.Add("@method", SqlDbType.NVarChar, 40).Value = method;
            sale.Parameters.Add("@notes", SqlDbType.NVarChar, 400).Value = totals.Discount == 0 ? DBNull.Value : $"Discount: {totals.Discount:0.00}";
            sale.Parameters.Add("@paid", SqlDbType.Decimal).Value = tender.AppliedLBP;
            sale.Parameters.Add("@remaining", SqlDbType.Decimal).Value = remaining;
            int saleId = Convert.ToInt32(sale.ExecuteScalar());

            foreach (SaleLine line in request.Lines)
            {
                if (line.ServiceId is int serviceId)
                {
                    using var service = new SqlCommand(@"SELECT Price FROM dbo.ServicesCatalog WITH (HOLDLOCK)
                        WHERE ServiceID=@service AND ISNULL(IsActive,1)=1", connection, transaction);
                    service.Parameters.Add("@service", SqlDbType.Int).Value = serviceId;
                    object? currentPrice = service.ExecuteScalar();
                    if (currentPrice is null || Convert.ToDecimal(currentPrice) != line.UnitPrice)
                        throw new InvalidOperationException($"Service {line.ProductName} changed. Refresh the sale and try again.");
                    using var serviceItem = new SqlCommand(@"INSERT INTO SaleItems
                        (SaleId,ProductId,ServiceId,Quantity,UnitPrice,ItemType)
                        VALUES (@sale,NULL,@service,@quantity,@price,'Service')", connection, transaction);
                    serviceItem.Parameters.Add("@sale", SqlDbType.Int).Value = saleId;
                    serviceItem.Parameters.Add("@service", SqlDbType.Int).Value = serviceId;
                    serviceItem.Parameters.Add("@quantity", SqlDbType.Int).Value = line.Quantity;
                    serviceItem.Parameters.Add("@price", SqlDbType.Decimal).Value = line.UnitPrice;
                    serviceItem.ExecuteNonQuery();
                    continue;
                }
                using var stock = new SqlCommand(@"UPDATE Products WITH (UPDLOCK, ROWLOCK)
                    SET StockQuantity = ISNULL(StockQuantity, 0) - @quantity
                    WHERE ProductId = @product AND ISNULL(StockQuantity, 0) >= @quantity", connection, transaction);
                stock.Parameters.Add("@quantity", SqlDbType.Int).Value = line.Quantity;
                stock.Parameters.Add("@product", SqlDbType.Int).Value = line.ProductId;
                if (stock.ExecuteNonQuery() != 1) throw new InsufficientStockException(line.ProductName);

                using var item = new SqlCommand(@"INSERT INTO SaleItems
                    (SaleId, ProductId, Quantity, UnitPrice, ItemType)
                    VALUES (@sale, @product, @quantity, @price, 'Product')", connection, transaction);
                item.Parameters.Add("@sale", SqlDbType.Int).Value = saleId;
                item.Parameters.Add("@product", SqlDbType.Int).Value = line.ProductId;
                item.Parameters.Add("@quantity", SqlDbType.Int).Value = line.Quantity;
                item.Parameters.Add("@price", SqlDbType.Decimal).Value = line.UnitPrice;
                item.ExecuteNonQuery();
            }

            string receiptNumber = $"RCP-{saleId:D8}";
            using var receipt = new SqlCommand(@"INSERT dbo.SaleTenderReceipts
                (SaleId,ReceiptNumber,ReceivedLBP,ReceivedUSD,LbpPerUsd,TenderTotalLBP,
                 AppliedLBP,ChangeLBP,ChangeUSD,PaymentMethod)
                VALUES (@sale,@number,@lbp,@usd,@rate,@tender,@applied,@change,@changeUsd,@method)", connection, transaction);
            receipt.Parameters.Add("@sale", SqlDbType.Int).Value = saleId;
            receipt.Parameters.Add("@number", SqlDbType.NVarChar, 30).Value = receiptNumber;
            receipt.Parameters.Add("@lbp", SqlDbType.Decimal).Value = tender.ReceivedLBP;
            receipt.Parameters.Add("@usd", SqlDbType.Decimal).Value = tender.ReceivedUSD;
            receipt.Parameters.Add("@rate", SqlDbType.Decimal).Value = tender.LbpPerUsd;
            receipt.Parameters.Add("@tender", SqlDbType.Decimal).Value = tender.TenderTotalLBP;
            receipt.Parameters.Add("@applied", SqlDbType.Decimal).Value = tender.AppliedLBP;
            receipt.Parameters.Add("@change", SqlDbType.Decimal).Value = tender.ChangeLBP;
            receipt.Parameters.Add("@changeUsd", SqlDbType.Decimal).Value = tender.ChangeUSD;
            receipt.Parameters.Add("@method", SqlDbType.NVarChar, 20).Value = method;
            receipt.ExecuteNonQuery();

            transaction.Commit();
            return new(saleId, invoice, totals, tender.AppliedLBP, remaining, receiptNumber, tender);
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }

    public void Void(int saleId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using var paymentCheck = new SqlCommand("SELECT COUNT(*) FROM dbo.CustomerPayments WHERE SaleId=@saleId AND ReversedAt IS NULL", connection, transaction);
            paymentCheck.Parameters.Add("@saleId", SqlDbType.Int).Value = saleId;
            if (Convert.ToInt32(paymentCheck.ExecuteScalar()) > 0)
                throw new InvalidOperationException("Reverse later payments before voiding this sale.");
            using var saleCheck = new SqlCommand("SELECT IsVoided FROM dbo.Sales WITH (UPDLOCK,HOLDLOCK) WHERE SaleId=@saleId", connection, transaction);
            saleCheck.Parameters.Add("@saleId", SqlDbType.Int).Value = saleId;
            object? current = saleCheck.ExecuteScalar();
            if (current is null) throw new InvalidOperationException("Sale no longer exists.");
            if (current is bool isVoided && isVoided) throw new InvalidOperationException("Sale is already voided.");
            using var restore = new SqlCommand(@"UPDATE p SET p.StockQuantity = ISNULL(p.StockQuantity, 0) + i.Quantity
                FROM Products p JOIN SaleItems i ON i.ProductId = p.ProductId WHERE i.SaleId = @saleId", connection, transaction);
            restore.Parameters.Add("@saleId", SqlDbType.Int).Value = saleId;
            restore.ExecuteNonQuery();
            using var markVoid = new SqlCommand("UPDATE dbo.Sales SET IsVoided=1,VoidedAt=SYSDATETIME() WHERE SaleId=@saleId AND IsVoided=0", connection, transaction);
            markVoid.Parameters.Add("@saleId", SqlDbType.Int).Value = saleId;
            if (markVoid.ExecuteNonQuery() != 1) throw new InvalidOperationException("Sale changed. Refresh and retry.");
            transaction.Commit();
        }
        catch { transaction.Rollback(); throw; }
    }
}
