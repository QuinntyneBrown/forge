// Acceptance Test
// Traces to: BT-013 (List equipment), L2-010
// Description: GET /api/equipment returns the four supported equipment types
// in stable enum order so the frontend can render the picker without
// hard-coding the list.

using System.Net.Http.Json;
using Forge.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Forge.Acceptance.Equipment;

public class ListEquipmentAcceptanceTest : IAsyncLifetime
{
    private readonly string _databaseName = $"Forge_Acceptance_{Guid.NewGuid():N}";
    private readonly string _connectionString;
    private WebApplicationFactory<Program>? _factory;

    public ListEquipmentAcceptanceTest()
    {
        _connectionString =
            $@"Server=.\SQLEXPRESS;Database={_databaseName};Trusted_Connection=True;TrustServerCertificate=True";
    }

    public async Task InitializeAsync()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(_connectionString)
            .Options;
        await using (var db = new AppDbContext(options))
        {
            await db.Database.MigrateAsync();
        }

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, config) =>
            {
                config.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = _connectionString
                });
            });
            builder.ConfigureTestServices(services =>
            {
                var dbDescriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (dbDescriptor is not null)
                {
                    services.Remove(dbDescriptor);
                }
                services.AddDbContext<AppDbContext>(opts => opts.UseSqlServer(_connectionString));
            });
        });

        using var client = _factory.CreateClient();
        var health = await client.GetAsync("/health");
        health.EnsureSuccessStatusCode();
    }

    public async Task DisposeAsync()
    {
        if (_factory is not null)
        {
            await _factory.DisposeAsync();
        }

        await using var conn = new SqlConnection(
            @"Server=.\SQLEXPRESS;Database=master;Trusted_Connection=True;TrustServerCertificate=True");
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

    private record EquipmentDto(string Id, string Name);

    [Fact]
    public async Task Returns_four_entries_in_stable_order()
    {
        var client = _factory!.CreateClient();
        var response = await client.GetAsync("/api/equipment");
        response.EnsureSuccessStatusCode();

        var items = await response.Content.ReadFromJsonAsync<EquipmentDto[]>();
        Assert.NotNull(items);
        Assert.Equal(4, items!.Length);

        Assert.Equal("Treadmill", items[0].Id);
        Assert.Equal("IndoorBike", items[1].Id);
        Assert.Equal("BenchPress", items[2].Id);
        Assert.Equal("Elliptical", items[3].Id);

        Assert.Equal("Treadmill", items[0].Name);
        Assert.Equal("Indoor Bike", items[1].Name);
        Assert.Equal("Bench Press", items[2].Name);
        Assert.Equal("Elliptical", items[3].Name);
    }
}
