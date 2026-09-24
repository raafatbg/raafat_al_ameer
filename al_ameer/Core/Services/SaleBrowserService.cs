using al_ameer.Data;
using Microsoft.Data.SqlClient;

namespace al_ameer.Services;

public sealed class SaleBrowserNode
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Kind { get; init; } = "Folder";
    public decimal Price { get; init; }
    public int Stock { get; init; }
    public string Icon => Kind switch { "Folder" => "📁", "Service" => "✦", _ => "▣" };
    public string Detail => Kind switch
    {
        "Product" => $"{Price:N0} LBP · {Stock} in stock",
        "Service" => $"{Price:N0} LBP",
        _ => $"{Children.Count} items"
    };
    public List<SaleBrowserNode> Children { get; } = [];
}

public sealed class SaleBrowserService
{
    private readonly string _connectionString;
    public SaleBrowserService(string? connectionString = null) =>
        _connectionString = connectionString ?? DatabaseConfig.ConnectionString;

    public IReadOnlyList<SaleBrowserNode> Load()
    {
        using var connection = new SqlConnection(_connectionString);
        connection.Open();
        var folders = new List<SaleBrowserNode>();
        var byCategory = new Dictionary<int, SaleBrowserNode>();
        using (var categories = new SqlCommand("SELECT CategoryId,CategoryName FROM dbo.Categories ORDER BY CategoryName", connection))
        using (var reader = categories.ExecuteReader())
            while (reader.Read())
            {
                var folder = new SaleBrowserNode { Id = reader.GetInt32(0), Name = reader.GetString(1) };
                folders.Add(folder);
                byCategory.Add(folder.Id, folder);
            }
        SaleBrowserNode? uncategorized = null;
        using (var products = new SqlCommand(@"SELECT ProductId,ProductName,CategoryId,SellingPrice,ISNULL(StockQuantity,0)
            FROM dbo.Products WHERE ISNULL(IsActive,1)=1 ORDER BY ProductName", connection))
        using (var reader = products.ExecuteReader())
            while (reader.Read())
            {
                var item = new SaleBrowserNode { Id = reader.GetInt32(0), Name = reader.GetString(1),
                    Kind = "Product", Price = reader.GetDecimal(3), Stock = reader.GetInt32(4) };
                if (!reader.IsDBNull(2) && byCategory.TryGetValue(reader.GetInt32(2), out var folder))
                    folder.Children.Add(item);
                else
                {
                    uncategorized ??= new SaleBrowserNode { Name = "Uncategorized" };
                    uncategorized.Children.Add(item);
                }
            }
        if (uncategorized is not null) folders.Add(uncategorized);
        var servicesFolder = new SaleBrowserNode { Name = "Services" };
        using (var services = new SqlCommand(@"SELECT ServiceID,ServiceName,Price FROM dbo.ServicesCatalog
            WHERE ISNULL(IsActive,1)=1 ORDER BY ServiceName", connection))
        using (var reader = services.ExecuteReader())
            while (reader.Read()) servicesFolder.Children.Add(new SaleBrowserNode
            { Id = reader.GetInt32(0), Name = reader.GetString(1), Kind = "Service", Price = reader.GetDecimal(2) });
        folders.Add(servicesFolder);
        return folders;
    }
}
