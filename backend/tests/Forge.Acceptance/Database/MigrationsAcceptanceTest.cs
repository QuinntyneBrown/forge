// Acceptance Test
// Traces to: BT-001 (Initial EF migration), L2-001, L2-002, L2-031..L2-038, L2-049
// Description: Run the migrations against a fresh LocalDB instance and assert
// that every required table exists by querying INFORMATION_SCHEMA.TABLES.

using System.Data;
using Forge.Infrastructure;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Forge.Acceptance.Database;

public class MigrationsAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;

    public MigrationsAcceptanceTest()
    {
        _connectionString =
            $@"Server=(localdb)\mssqllocaldb;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public async Task DisposeAsync()
    {
        await using var conn = new SqlConnection(
            @"Server=(localdb)\mssqllocaldb;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
            IF DB_ID('{_databaseName}') IS NOT NULL
            BEGIN
                ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{_databaseName}];
            END";
        await cmd.ExecuteNonQueryAsync();
    }

    [Fact]
    public async Task Migrations_create_every_required_table_on_a_fresh_database()
    {
        // Apply migrations from a freshly constructed DbContext — same code path
        // Program.cs uses on startup (db.Database.MigrateAsync()).
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using (var db = new AppDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        var tables = await GetTablesAsync(_connectionString);

        var required = new[]
        {
            "Users",
            "WorkoutSessions",
            "RefreshTokens",
            "SignInAttempts",
            "AuditLogs",
            "PasswordResetTokens",
            "__EFMigrationsHistory"
        };

        var missing = required.Where(t => !tables.Contains(t, StringComparer.OrdinalIgnoreCase)).ToArray();
        Assert.True(missing.Length == 0,
            $"Missing tables: [{string.Join(", ", missing)}]. Found tables: [{string.Join(", ", tables)}]");
    }

    private static async Task<HashSet<string>> GetTablesAsync(string connectionString)
    {
        var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();
        await using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE'";
        await using var reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
        while (await reader.ReadAsync())
        {
            tables.Add(reader.GetString(0));
        }
        return tables;
    }
}
