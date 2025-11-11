using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaterService.Migrations
{
    /// <inheritdoc />
    public partial class AddTierPriceAndAppliedTierPriceId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Create TierPrices table if it doesn't exist
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS ""TierPrices"" (
                    ""Id"" INTEGER NOT NULL CONSTRAINT ""PK_TierPrices"" PRIMARY KEY AUTOINCREMENT,
                    ""Tier1Price"" TEXT NOT NULL,
                    ""Tier2Price"" TEXT NOT NULL,
                    ""Tier3Price"" TEXT NOT NULL,
                    ""CreatedAt"" TEXT NOT NULL,
                    ""UpdatedAt"" TEXT NOT NULL
                );
            ");

            // Add AppliedTierPriceId column to Invoices table
            migrationBuilder.AddColumn<int>(
                name: "AppliedTierPriceId",
                table: "Invoices",
                type: "INTEGER",
                nullable: true);

            // Create index for AppliedTierPriceId
            migrationBuilder.CreateIndex(
                name: "IX_Invoices_AppliedTierPriceId",
                table: "Invoices",
                column: "AppliedTierPriceId");

            // Add foreign key constraint
            migrationBuilder.AddForeignKey(
                name: "FK_Invoices_TierPrices_AppliedTierPriceId",
                table: "Invoices",
                column: "AppliedTierPriceId",
                principalTable: "TierPrices",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove foreign key
            migrationBuilder.DropForeignKey(
                name: "FK_Invoices_TierPrices_AppliedTierPriceId",
                table: "Invoices");

            // Remove index
            migrationBuilder.DropIndex(
                name: "IX_Invoices_AppliedTierPriceId",
                table: "Invoices");

            // Remove column
            migrationBuilder.DropColumn(
                name: "AppliedTierPriceId",
                table: "Invoices");

            // Drop table
            migrationBuilder.DropTable(
                name: "TierPrices");
        }
    }
}
