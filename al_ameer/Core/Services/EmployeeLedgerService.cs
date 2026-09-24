using al_ameer.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace al_ameer.Services;

public sealed record EmployeeRecord(int EmployeeId, string FullName, string? Phone, string? JobTitle,
    DateTime HireDate, decimal MonthlySalary, bool IsActive, decimal SalaryBalance);
public sealed record SalaryEntry(int SalaryEntryId, int EmployeeId, DateTime EntryDate,
    string EntryType, decimal Amount, string? Notes, DateTime? ReversedAt, string? ReversalReason);

public sealed class EmployeeLedgerService
{
    private readonly string _connectionString;
    public EmployeeLedgerService(string? connectionString = null) =>
        _connectionString = connectionString ?? DatabaseConfig.ConnectionString;

    public IReadOnlyList<EmployeeRecord> GetEmployees()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"SELECT e.EmployeeId,e.FullName,e.Phone,e.JobTitle,e.HireDate,
            e.MonthlySalary,e.IsActive,COALESCE(SUM(CASE WHEN l.ReversedAt IS NOT NULL THEN 0 WHEN l.EntryType='Due' THEN l.Amount ELSE -l.Amount END),0)
            FROM dbo.Employees e LEFT JOIN dbo.EmployeeSalaryEntries l ON l.EmployeeId=e.EmployeeId
            GROUP BY e.EmployeeId,e.FullName,e.Phone,e.JobTitle,e.HireDate,e.MonthlySalary,e.IsActive
            ORDER BY e.IsActive DESC,e.FullName", connection);
        using var reader = command.ExecuteReader();
        var employees = new List<EmployeeRecord>();
        while (reader.Read()) employees.Add(new(reader.GetInt32(0), reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3),
            reader.GetDateTime(4), reader.GetDecimal(5), reader.GetBoolean(6), reader.GetDecimal(7)));
        return employees;
    }

    public IReadOnlyList<SalaryEntry> GetEntries(int employeeId)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"SELECT SalaryEntryId,EmployeeId,EntryDate,EntryType,Amount,Notes,ReversedAt,ReversalReason
            FROM dbo.EmployeeSalaryEntries WHERE EmployeeId=@id ORDER BY EntryDate DESC,SalaryEntryId DESC", connection);
        command.Parameters.Add("@id", SqlDbType.Int).Value = employeeId;
        using var reader = command.ExecuteReader();
        var entries = new List<SalaryEntry>();
        while (reader.Read()) entries.Add(new(reader.GetInt32(0), reader.GetInt32(1), reader.GetDateTime(2),
            reader.GetString(3), reader.GetDecimal(4), reader.IsDBNull(5) ? null : reader.GetString(5),
            reader.IsDBNull(6) ? null : reader.GetDateTime(6), reader.IsDBNull(7) ? null : reader.GetString(7)));
        return entries;
    }

    public int SaveEmployee(int? employeeId, string fullName, string? phone, string? jobTitle,
        DateTime hireDate, decimal monthlySalary, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(fullName) || fullName.Length > 100) throw new ArgumentException("Enter an employee name up to 100 characters.");
        if (phone?.Length > 40 || jobTitle?.Length > 100) throw new ArgumentException("Phone or job title is too long.");
        if (monthlySalary < 0) throw new ArgumentOutOfRangeException(nameof(monthlySalary));
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        string sql = employeeId is null
            ? @"INSERT dbo.Employees (FullName,Phone,JobTitle,HireDate,MonthlySalary,IsActive)
                OUTPUT INSERTED.EmployeeId VALUES (@name,@phone,@title,@hire,@salary,@active)"
            : @"UPDATE dbo.Employees SET FullName=@name,Phone=@phone,JobTitle=@title,HireDate=@hire,
                MonthlySalary=@salary,IsActive=@active OUTPUT INSERTED.EmployeeId WHERE EmployeeId=@id";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@name", SqlDbType.NVarChar, 100).Value = fullName.Trim();
        command.Parameters.Add("@phone", SqlDbType.NVarChar, 40).Value = string.IsNullOrWhiteSpace(phone) ? DBNull.Value : phone.Trim();
        command.Parameters.Add("@title", SqlDbType.NVarChar, 100).Value = string.IsNullOrWhiteSpace(jobTitle) ? DBNull.Value : jobTitle.Trim();
        command.Parameters.Add("@hire", SqlDbType.Date).Value = hireDate.Date;
        command.Parameters.Add("@salary", SqlDbType.Decimal).Value = monthlySalary;
        command.Parameters.Add("@active", SqlDbType.Bit).Value = isActive;
        if (employeeId.HasValue) command.Parameters.Add("@id", SqlDbType.Int).Value = employeeId.Value;
        return Convert.ToInt32(command.ExecuteScalar() ?? throw new InvalidOperationException("Employee no longer exists."));
    }

    public void AddEntry(int employeeId, DateTime date, string entryType, decimal amount, string? notes)
    {
        if (entryType is not ("Due" or "Payment")) throw new ArgumentException("Invalid salary entry type.");
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Amount must be greater than zero.");
        if (notes?.Length > 400) throw new ArgumentException("Notes cannot exceed 400 characters.");
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using var lockEmployee = new SqlCommand("SELECT IsActive,FullName FROM dbo.Employees WITH (UPDLOCK,HOLDLOCK) WHERE EmployeeId=@id", connection, transaction);
            lockEmployee.Parameters.Add("@id", SqlDbType.Int).Value = employeeId;
            string employeeName;
            using (var reader = lockEmployee.ExecuteReader())
            {
                if (!reader.Read()) throw new InvalidOperationException("Employee no longer exists.");
                if (!reader.GetBoolean(0)) throw new InvalidOperationException("Reactivate this employee before posting salary entries.");
                employeeName = reader.GetString(1);
            }
            if (entryType == "Payment")
            {
                using var balanceCommand = new SqlCommand(@"SELECT COALESCE(SUM(CASE WHEN EntryType='Due' THEN Amount ELSE -Amount END),0)
                    FROM dbo.EmployeeSalaryEntries WHERE EmployeeId=@id AND ReversedAt IS NULL", connection, transaction);
                balanceCommand.Parameters.Add("@id", SqlDbType.Int).Value = employeeId;
                decimal balance = Convert.ToDecimal(balanceCommand.ExecuteScalar());
                if (amount > balance) throw new InvalidOperationException("Payment exceeds salary due.");
            }
            using var insert = new SqlCommand(@"INSERT dbo.EmployeeSalaryEntries (EmployeeId,EntryDate,EntryType,Amount,Notes)
                OUTPUT INSERTED.SalaryEntryId VALUES (@id,@date,@type,@amount,@notes)", connection, transaction);
            insert.Parameters.Add("@id", SqlDbType.Int).Value = employeeId;
            insert.Parameters.Add("@date", SqlDbType.Date).Value = date.Date;
            insert.Parameters.Add("@type", SqlDbType.NVarChar, 10).Value = entryType;
            insert.Parameters.Add("@amount", SqlDbType.Decimal).Value = amount;
            insert.Parameters.Add("@notes", SqlDbType.NVarChar, 400).Value = string.IsNullOrWhiteSpace(notes) ? DBNull.Value : notes.Trim();
            int entryId = Convert.ToInt32(insert.ExecuteScalar());
            if (entryType == "Payment")
            {
                using var expense = new SqlCommand(@"INSERT dbo.Expenses
                    (Category,Amount,Description,ExpenseDate,SalaryEntryId)
                    VALUES (N'Salary',@amount,@description,@date,@entry)", connection, transaction);
                expense.Parameters.Add("@amount", SqlDbType.Decimal).Value = amount;
                expense.Parameters.Add("@description", SqlDbType.NVarChar, -1).Value =
                    $"Salary payment — {employeeName}" + (string.IsNullOrWhiteSpace(notes) ? "" : $" — {notes.Trim()}");
                expense.Parameters.Add("@date", SqlDbType.DateTime).Value = date.Date;
                expense.Parameters.Add("@entry", SqlDbType.Int).Value = entryId;
                expense.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch { transaction.Rollback(); throw; }
    }

    public void ReverseEntry(int employeeId, int entryId, string reason)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 400)
            throw new ArgumentException("Enter a reversal reason up to 400 characters.");
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var transaction = connection.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            using var lockEmployee = new SqlCommand("SELECT EmployeeId FROM dbo.Employees WITH (UPDLOCK,HOLDLOCK) WHERE EmployeeId=@id", connection, transaction);
            lockEmployee.Parameters.Add("@id", SqlDbType.Int).Value = employeeId;
            if (lockEmployee.ExecuteScalar() is null) throw new InvalidOperationException("Employee not found.");
            using var select = new SqlCommand(@"SELECT EntryType,Amount,ReversedAt FROM dbo.EmployeeSalaryEntries WITH (UPDLOCK,HOLDLOCK)
                WHERE EmployeeId=@employee AND SalaryEntryId=@entry", connection, transaction);
            select.Parameters.Add("@employee", SqlDbType.Int).Value = employeeId;
            select.Parameters.Add("@entry", SqlDbType.Int).Value = entryId;
            string type; decimal amount;
            using (var reader = select.ExecuteReader())
            {
                if (!reader.Read()) throw new InvalidOperationException("Salary entry not found.");
                if (!reader.IsDBNull(2)) throw new InvalidOperationException("Entry is already reversed.");
                type = reader.GetString(0); amount = reader.GetDecimal(1);
            }
            if (type == "Due")
            {
                using var balance = new SqlCommand(@"SELECT COALESCE(SUM(CASE WHEN EntryType='Due' THEN Amount ELSE -Amount END),0)
                    FROM dbo.EmployeeSalaryEntries WHERE EmployeeId=@id AND ReversedAt IS NULL", connection, transaction);
                balance.Parameters.Add("@id", SqlDbType.Int).Value = employeeId;
                if (Convert.ToDecimal(balance.ExecuteScalar()) < amount)
                    throw new InvalidOperationException("Reverse related salary payments first; this due has already been paid.");
            }
            using var update = new SqlCommand(@"UPDATE dbo.EmployeeSalaryEntries SET ReversedAt=SYSDATETIME(),
                ReversalReason=@reason WHERE SalaryEntryId=@entry AND EmployeeId=@employee AND ReversedAt IS NULL", connection, transaction);
            update.Parameters.Add("@reason", SqlDbType.NVarChar, 400).Value = reason.Trim();
            update.Parameters.Add("@entry", SqlDbType.Int).Value = entryId;
            update.Parameters.Add("@employee", SqlDbType.Int).Value = employeeId;
            if (update.ExecuteNonQuery() != 1) throw new InvalidOperationException("Entry changed. Refresh and retry.");
            if (type == "Payment")
            {
                using var reverseExpense = new SqlCommand(@"UPDATE dbo.Expenses SET ReversedAt=SYSDATETIME(),
                    ReversalReason=@reason WHERE SalaryEntryId=@entry AND ReversedAt IS NULL", connection, transaction);
                reverseExpense.Parameters.Add("@reason", SqlDbType.NVarChar, 400).Value = reason.Trim();
                reverseExpense.Parameters.Add("@entry", SqlDbType.Int).Value = entryId;
                reverseExpense.ExecuteNonQuery();
            }
            transaction.Commit();
        }
        catch { transaction.Rollback(); throw; }
    }
}
