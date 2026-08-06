using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BV.Persistence.Migrations;

public partial class AddOrderStatusHistory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "OrderStatusHistories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FromStatus = table.Column<int>(type: "int", nullable: false),
                ToStatus = table.Column<int>(type: "int", nullable: false),
                Note = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                ChangedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ChangedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderStatusHistories", x => x.Id);
                table.ForeignKey(
                    name: "FK_OrderStatusHistories_Orders_OrderId",
                    column: x => x.OrderId,
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OrderStatusHistories_OrderId_ChangedAtUtc",
            table: "OrderStatusHistories",
            columns: new[] { "OrderId", "ChangedAtUtc" });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OrderStatusHistories");
    }
}
