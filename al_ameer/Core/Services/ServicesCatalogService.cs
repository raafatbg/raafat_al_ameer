using al_ameer.Data;
using Microsoft.Data.SqlClient;
using System.Data;

namespace al_ameer.Services;

public sealed record ServiceCatalogItem(int ServiceId, string ServiceName, string? Description,
    decimal Price, bool IsActive);

public sealed class ServicesCatalogService
{
    private readonly string _connectionString;
    public ServicesCatalogService(string? connectionString = null) =>
        _connectionString = connectionString ?? DatabaseConfig.ConnectionString;

    public IReadOnlyList<ServiceCatalogItem> GetAll(bool activeOnly = false)
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        using var command = new SqlCommand(@"SELECT ServiceID,ServiceName,ServiceDescription,Price,ISNULL(IsActive,1)
            FROM dbo.ServicesCatalog WHERE @activeOnly=0 OR ISNULL(IsActive,1)=1 ORDER BY ServiceName", connection);
        command.Parameters.Add("@activeOnly", SqlDbType.Bit).Value = activeOnly;
        using var reader = command.ExecuteReader();
        var items = new List<ServiceCatalogItem>();
        while (reader.Read()) items.Add(new(reader.GetInt32(0), reader.GetString(1),
            reader.IsDBNull(2) ? null : reader.GetString(2), reader.GetDecimal(3), reader.GetBoolean(4)));
        return items;
    }

    public int Save(int? id, string name, string? description, decimal price, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150)
            throw new ArgumentException("Enter a service name up to 150 characters.");
        if (price < 0) throw new ArgumentOutOfRangeException(nameof(price));
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        string sql = id is null
            ? @"INSERT dbo.ServicesCatalog (ServiceName,ServiceDescription,Price,IsActive)
                OUTPUT INSERTED.ServiceID VALUES (@name,@description,@price,@active)"
            : @"UPDATE dbo.ServicesCatalog SET ServiceName=@name,ServiceDescription=@description,
                Price=@price,IsActive=@active OUTPUT INSERTED.ServiceID WHERE ServiceID=@id";
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@name", SqlDbType.NVarChar, 150).Value = name.Trim();
        command.Parameters.Add("@description", SqlDbType.NVarChar, -1).Value =
            string.IsNullOrWhiteSpace(description) ? DBNull.Value : description.Trim();
        command.Parameters.Add("@price", SqlDbType.Decimal).Value = price;
        command.Parameters.Add("@active", SqlDbType.Bit).Value = isActive;
        if (id.HasValue) command.Parameters.Add("@id", SqlDbType.Int).Value = id.Value;
        return Convert.ToInt32(command.ExecuteScalar() ?? throw new InvalidOperationException("Service no longer exists."));
    }
}
