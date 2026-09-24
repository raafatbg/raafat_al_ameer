using Microsoft.Data.SqlClient;

namespace al_ameer.Data;

public static class DatabaseConfig
{
    private const string LaptopName = "WIN-LP90440U1LA";

    public static string ConnectionString => BuildConnectionString(
        Environment.MachineName,
        Environment.GetEnvironmentVariable("AL_AMEER_SQL_SERVER"));

    public static string BuildConnectionString(string machineName, string? serverOverride = null)
    {
        string server = !string.IsNullOrWhiteSpace(serverOverride)
            ? serverOverride.Trim()
            : string.Equals(machineName, LaptopName, StringComparison.OrdinalIgnoreCase)
                ? LaptopName
                : @".\MSSQLSERVER03";

        return new SqlConnectionStringBuilder
        {
            DataSource = server,
            InitialCatalog = "al_ameer",
            IntegratedSecurity = true,
            TrustServerCertificate = true,
            Encrypt = SqlConnectionEncryptOption.Optional
        }.ConnectionString;
    }
}
