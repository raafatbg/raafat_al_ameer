using al_ameer.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace al_ameer.Services;

public sealed record AgingRow(string Customer, decimal Current, decimal Days31To60, decimal Days61To90, decimal Over90, decimal Total);
public sealed record SalaryBalanceRow(string Employee, decimal Due, decimal Paid, decimal Balance);
public sealed record CashFlowRow(DateTime Date, decimal Inflow, decimal Expenses, decimal SalaryPayments, decimal Net);

public sealed class FinancialReportService
{
    private readonly string _connectionString;
    public FinancialReportService(string? connectionString = null) => _connectionString = connectionString ?? DatabaseConfig.ConnectionString;

    public IReadOnlyList<AgingRow> GetCustomerAging(DateTime asOf)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"SELECT c.FullName,
            COALESCE(SUM(CASE WHEN DATEDIFF(day,CAST(s.SaleDate AS date),@asOf)<=30 THEN s.RemainingBalance ELSE 0 END),0),
            COALESCE(SUM(CASE WHEN DATEDIFF(day,CAST(s.SaleDate AS date),@asOf) BETWEEN 31 AND 60 THEN s.RemainingBalance ELSE 0 END),0),
            COALESCE(SUM(CASE WHEN DATEDIFF(day,CAST(s.SaleDate AS date),@asOf) BETWEEN 61 AND 90 THEN s.RemainingBalance ELSE 0 END),0),
            COALESCE(SUM(CASE WHEN DATEDIFF(day,CAST(s.SaleDate AS date),@asOf)>90 THEN s.RemainingBalance ELSE 0 END),0),
            COALESCE(SUM(s.RemainingBalance),0)
            FROM dbo.Customers c JOIN dbo.Sales s ON s.CustomerId=c.CustomerId
            WHERE s.IsVoided=0 AND s.RemainingBalance>0 AND s.SaleDate<DATEADD(day,1,@asOf)
            GROUP BY c.CustomerId,c.FullName ORDER BY c.FullName", connection);
        command.Parameters.Add("@asOf", SqlDbType.Date).Value = asOf.Date;
        using var reader = command.ExecuteReader();
        var rows = new List<AgingRow>();
        while (reader.Read()) rows.Add(new(reader.GetString(0), reader.GetDecimal(1), reader.GetDecimal(2), reader.GetDecimal(3), reader.GetDecimal(4), reader.GetDecimal(5)));
        return rows;
    }

    public IReadOnlyList<SalaryBalanceRow> GetSalaryBalances()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"SELECT e.FullName,
            COALESCE(SUM(CASE WHEN l.EntryType='Due' AND l.ReversedAt IS NULL THEN l.Amount ELSE 0 END),0),
            COALESCE(SUM(CASE WHEN l.EntryType='Payment' AND l.ReversedAt IS NULL THEN l.Amount ELSE 0 END),0)
            FROM dbo.Employees e LEFT JOIN dbo.EmployeeSalaryEntries l ON l.EmployeeId=e.EmployeeId
            GROUP BY e.EmployeeId,e.FullName ORDER BY e.FullName", connection);
        using var reader = command.ExecuteReader();
        var rows = new List<SalaryBalanceRow>();
        while (reader.Read()) { decimal due = reader.GetDecimal(1), paid = reader.GetDecimal(2); rows.Add(new(reader.GetString(0), due, paid, due - paid)); }
        return rows;
    }

    public IReadOnlyList<CashFlowRow> GetCashFlow(DateTime from, DateTime through)
    {
        if (through.Date < from.Date) throw new ArgumentException("End date must be on or after start date.");
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"SELECT [Date],SUM(Inflow),SUM(Expenses),SUM(SalaryPayments)
            FROM (
              SELECT CAST(s.SaleDate AS date) [Date],CAST(s.PaidAmount-COALESCE(p.ActivePayments,0) AS decimal(18,2)) Inflow,
                CAST(0 AS decimal(18,2)) Expenses,CAST(0 AS decimal(18,2)) SalaryPayments
                FROM dbo.Sales s OUTER APPLY
                  (SELECT SUM(cp.Amount) ActivePayments FROM dbo.CustomerPayments cp WHERE cp.SaleId=s.SaleId AND cp.ReversedAt IS NULL) p
                WHERE s.IsVoided=0
              UNION ALL SELECT CAST(PaymentDate AS date),Amount,0,0 FROM dbo.CustomerPayments WHERE ReversedAt IS NULL
              UNION ALL SELECT CAST(ExpenseDate AS date),0,COALESCE(Amount,0),0 FROM dbo.Expenses
                WHERE SalaryEntryId IS NULL AND ReversedAt IS NULL
              UNION ALL SELECT EntryDate,0,0,Amount FROM dbo.EmployeeSalaryEntries WHERE EntryType='Payment' AND ReversedAt IS NULL
            ) x WHERE [Date] BETWEEN @from AND @through
            GROUP BY [Date] ORDER BY [Date] DESC", connection);
        command.Parameters.Add("@from", SqlDbType.Date).Value = from.Date;
        command.Parameters.Add("@through", SqlDbType.Date).Value = through.Date;
        using var reader = command.ExecuteReader();
        var rows = new List<CashFlowRow>();
        while (reader.Read()) { decimal inflow = reader.GetDecimal(1), expenses = reader.GetDecimal(2), salary = reader.GetDecimal(3); rows.Add(new(reader.GetDateTime(0), inflow, expenses, salary, inflow - expenses - salary)); }
        return rows;
    }
}
