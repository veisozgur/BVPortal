using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BV.Persistence.Migrations;

public partial class AddQuoteOperationNotes : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "QuoteOperationNotes",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QuoteRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedByUserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Note = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuoteOperationNotes", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_QuoteOperationNotes_CreatedAtUtc",
            table: "QuoteOperationNotes",
            column: "CreatedAtUtc");

        migrationBuilder.CreateIndex(
            name: "IX_QuoteOperationNotes_QuoteRequestId",
            table: "QuoteOperationNotes",
            column: "QuoteRequestId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "QuoteOperationNotes");
    }
}
