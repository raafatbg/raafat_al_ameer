using al_ameer.Data;
using Microsoft.Data.SqlClient;

namespace al_ameer.Tests;

public class DatabaseConfigTests
{
    [Fact]
    public void LaptopUsesItsLocalDefaultSqlServer()
    {
        var connection = new SqlConnectionStringBuilder(
            DatabaseConfig.BuildConnectionString("WIN-LP90440U1LA"));
        Assert.Equal("WIN-LP90440U1LA", connection.DataSource);
        Assert.Equal("al_ameer", connection.InitialCatalog);
        Assert.True(connection.IntegratedSecurity);
    }

    [Fact]
    public void ExistingDevelopmentComputerKeepsItsNamedInstance()
    {
        var connection = new SqlConnectionStringBuilder(
            DatabaseConfig.BuildConnectionString("DESKTOP-TVOR3BK"));
        Assert.Equal(@".\MSSQLSERVER03", connection.DataSource);
    }

    [Fact]
    public void ExplicitServerOverrideTakesPriority()
    {
        var connection = new SqlConnectionStringBuilder(
            DatabaseConfig.BuildConnectionString("WIN-LP90440U1LA", @".\SQLEXPRESS"));
        Assert.Equal(@".\SQLEXPRESS", connection.DataSource);
    }
}
