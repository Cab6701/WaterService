using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WaterService.Migrations
{
    /// <inheritdoc />
    public partial class EnableMeterReadingCascadeDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MeterReadings_Customers_CustomerId",
                table: "MeterReadings");

            migrationBuilder.DropColumn(
                name: "CustomerCode",
                table: "MeterReadings");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "MeterReadings",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_MeterReadings_Customers_CustomerId",
                table: "MeterReadings",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MeterReadings_Customers_CustomerId",
                table: "MeterReadings");

            migrationBuilder.AlterColumn<int>(
                name: "CustomerId",
                table: "MeterReadings",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "CustomerCode",
                table: "MeterReadings",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddForeignKey(
                name: "FK_MeterReadings_Customers_CustomerId",
                table: "MeterReadings",
                column: "CustomerId",
                principalTable: "Customers",
                principalColumn: "Id");
        }
    }
}
