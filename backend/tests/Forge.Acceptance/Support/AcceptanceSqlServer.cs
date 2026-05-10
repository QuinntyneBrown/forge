using Microsoft.Data.SqlClient;

namespace Forge.Acceptance.Support;

internal static class AcceptanceSqlServer
{
    private const string ConnectionStringEnvironmentVariable = "FORGE_ACCEPTANCE_SQLSERVER";
    private const string DefaultConnectionString =
        @"Server=.\SQLEXPRESS;Trusted_Connection=True;TrustServerCertificate=True";

    public static string MasterConnectionString => ForDatabase("master");

    public static string ForDatabase(string databaseName)
    {
        var builder = new SqlConnectionStringBuilder(BaseConnectionString)
        {
            InitialCatalog = databaseName
        };

        return builder.ConnectionString;
    }

    private static string BaseConnectionString =>
        Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable)
        ?? DefaultConnectionString;
}
