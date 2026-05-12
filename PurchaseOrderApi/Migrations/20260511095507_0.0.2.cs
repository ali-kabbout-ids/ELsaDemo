using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PurchaseOrderApi.Migrations
{
    /// <inheritdoc />
    public partial class _002 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MokhatabatRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationRequestId = table.Column<int>(type: "int", nullable: false),
                    WorkflowInstanceId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Step1Decision = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Step1Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Step1DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Step2Decision = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Step2Reason = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Step2DecidedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MokhatabatRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MokhatabatRequests_ApplicationRequests_ApplicationRequestId",
                        column: x => x.ApplicationRequestId,
                        principalTable: "ApplicationRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_MokhatabatRequests_ApplicationRequestId",
                table: "MokhatabatRequests",
                column: "ApplicationRequestId");

            migrationBuilder.CreateIndex(
                name: "IX_MokhatabatRequests_WorkflowInstanceId",
                table: "MokhatabatRequests",
                column: "WorkflowInstanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MokhatabatRequests");
        }
    }
}
