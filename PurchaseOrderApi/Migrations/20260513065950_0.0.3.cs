using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurchaseOrderApi.Migrations
{
    /// <inheritdoc />
    public partial class _003 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CurrentRequiredRole",
                table: "ApplicationRequests",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CurrentStepName",
                table: "ApplicationRequests",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CurrentRequiredRole",
                table: "ApplicationRequests");

            migrationBuilder.DropColumn(
                name: "CurrentStepName",
                table: "ApplicationRequests");
        }
    }
}
