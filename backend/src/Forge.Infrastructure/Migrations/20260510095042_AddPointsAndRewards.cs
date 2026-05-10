using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace Forge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPointsAndRewards : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PointsLedger",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SessionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    RedemptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Reason = table.Column<int>(type: "int", nullable: false),
                    Points = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PointsLedger", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RewardCatalogItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(400)", maxLength: 400, nullable: false),
                    CostPoints = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardCatalogItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RewardRedemptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RewardCatalogItemId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CostPoints = table.Column<int>(type: "int", nullable: false),
                    RedeemedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardRedemptions", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "RewardCatalogItems",
                columns: new[] { "Id", "CostPoints", "Description", "IsActive", "Name", "SortOrder" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-000000000001"), 200, "Trade points for a guilt-free recovery smoothie.", true, "Post-workout Smoothie", 1 },
                    { new Guid("11111111-1111-1111-1111-000000000002"), 500, "Skip a session without breaking your streak.", true, "Rest Day Pass", 2 },
                    { new Guid("11111111-1111-1111-1111-000000000003"), 750, "Treat yourself to a fresh pair of training socks.", true, "New Athletic Socks", 3 },
                    { new Guid("11111111-1111-1111-1111-000000000004"), 4000, "Replace your gym earbuds with a fresh set.", true, "Pair of Wireless Earbuds", 4 },
                    { new Guid("11111111-1111-1111-1111-000000000005"), 12000, "Upgrade the kicks once you bank enough.", true, "New Running Shoes", 5 },
                    { new Guid("11111111-1111-1111-1111-000000000006"), 8000, "Recover with a one-hour deep tissue massage.", true, "Massage Session", 6 },
                    { new Guid("11111111-1111-1111-1111-000000000007"), 6000, "5 lb tub of premium whey.", true, "Premium Whey Protein", 7 }
                });

            migrationBuilder.CreateIndex(
                name: "IX_PointsLedger_SessionId",
                table: "PointsLedger",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_PointsLedger_UserId_CreatedAt",
                table: "PointsLedger",
                columns: new[] { "UserId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_RewardCatalogItems_SortOrder",
                table: "RewardCatalogItems",
                column: "SortOrder");

            migrationBuilder.CreateIndex(
                name: "IX_RewardRedemptions_UserId_RedeemedAt",
                table: "RewardRedemptions",
                columns: new[] { "UserId", "RedeemedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PointsLedger");

            migrationBuilder.DropTable(
                name: "RewardCatalogItems");

            migrationBuilder.DropTable(
                name: "RewardRedemptions");
        }
    }
}
