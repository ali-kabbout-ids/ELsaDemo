using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurchaseOrderApi.Migrations
{
    /// <inheritdoc />
    public partial class _001 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransactionType = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    EmployeeEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    I3almKanouniEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Mo3awenCho3baEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    RequiresMo5atabat = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ReviewRound = table.Column<int>(type: "int", nullable: false),
                    I3almKanouniDecision = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    I3almKanouniReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Mo3awenDecision = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Mo3awenReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Mo5atabatDecision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FinalMo3awenDecision = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HasMane3Decision = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    HasMane3Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    WorkflowInstanceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationRequests", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApplicationRequests_WorkflowInstanceId",
                table: "ApplicationRequests",
                column: "WorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationRequests");
        }
    }
}
