using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaterService.Migrations
{
    /// <inheritdoc />
    public partial class RemoveUnitPriceFromMeterReadings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Remove UnitPrice column from MeterReadings table
            // This column is no longer needed as we now use TierPrice system
            // Note: For SQLite versions < 3.35.0, this may require manual migration
            // EF Core will handle DROP COLUMN for SQLite 3.35.0+
            migrationBuilder.DropColumn(
                name: "UnitPrice",
                table: "MeterReadings");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Add UnitPrice column back if migration needs to be rolled back
            migrationBuilder.AddColumn<decimal>(
                name: "UnitPrice",
                table: "MeterReadings",
                type: "TEXT",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
