using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BV.Persistence.Migrations;

public partial class AddQuoteResponsesAndNotifications : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "QuoteResponses",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QuoteRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Message = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                ValidUntilUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuoteResponses", x => x.Id);
                table.ForeignKey(
                    name: "FK_QuoteResponses_QuoteRequests_QuoteRequestId",
                    column: x => x.QuoteRequestId,
                    principalTable: "QuoteRequests",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "QuoteNotifications",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QuoteRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Channel = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Destination = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                ErrorMessage = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                SentAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_QuoteNotifications", x => x.Id));

        migrationBuilder.CreateTable(
            name: "QuoteResponseItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QuoteResponseId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                VatRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuoteResponseItems", x => x.Id);
                table.ForeignKey(
                    name: "FK_QuoteResponseItems_QuoteResponses_QuoteResponseId",
                    column: x => x.QuoteResponseId,
                    principalTable: "QuoteResponses",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_QuoteResponses_QuoteRequestId", table: "QuoteResponses", column: "QuoteRequestId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_QuoteResponseItems_QuoteResponseId", table: "QuoteResponseItems", column: "QuoteResponseId");
        migrationBuilder.CreateIndex(name: "IX_QuoteNotifications_QuoteRequestId", table: "QuoteNotifications", column: "QuoteRequestId");
        migrationBuilder.CreateIndex(name: "IX_QuoteNotifications_Status", table: "QuoteNotifications", column: "Status");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "QuoteNotifications");
        migrationBuilder.DropTable(name: "QuoteResponseItems");
        migrationBuilder.DropTable(name: "QuoteResponses");
    }
}
