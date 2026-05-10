using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Forge.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddProfileColumns : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DailyActiveCaloriesTarget",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 1500);

            migrationBuilder.AddColumn<int>(
                name: "DailyWorkoutMinutesTarget",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 60);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "KitchenClosedEnd",
                table: "Users",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(6, 0, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "KitchenClosedStart",
                table: "Users",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(20, 0, 0));

            migrationBuilder.AddColumn<bool>(
                name: "KitchenNudgeEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<bool>(
                name: "LeaderboardOptIn",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "MonthlyWeightGoalLb",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 20);

            migrationBuilder.AddColumn<bool>(
                name: "MorningReminderEnabled",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<TimeOnly>(
                name: "MorningWindowEnd",
                table: "Users",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(7, 30, 0));

            migrationBuilder.AddColumn<TimeOnly>(
                name: "MorningWindowStart",
                table: "Users",
                type: "time",
                nullable: false,
                defaultValue: new TimeOnly(5, 0, 0));

            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Users",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "America/New_York");

            migrationBuilder.AddColumn<string>(
                name: "Units",
                table: "Users",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "Imperial");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DailyActiveCaloriesTarget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "DailyWorkoutMinutesTarget",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KitchenClosedEnd",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KitchenClosedStart",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "KitchenNudgeEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "LeaderboardOptIn",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MonthlyWeightGoalLb",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MorningReminderEnabled",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MorningWindowEnd",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MorningWindowStart",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "Units",
                table: "Users");
        }
    }
}
