using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BV.Persistence.Migrations;

public partial class AddCustomersAndQuoteRequests : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "CustomerProfiles",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                FullName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                PhoneNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                Email = table.Column<string>(type: "nvarchar(320)", maxLength: 320, nullable: false),
                OrganizationName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                TaxNumber = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true),
                Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                City = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                District = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_CustomerProfiles", x => x.Id));

        migrationBuilder.CreateTable(
            name: "QuoteRequests",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                SubmittedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuoteRequests", x => x.Id);
                table.ForeignKey("FK_QuoteRequests_CustomerProfiles_CustomerId", x => x.CustomerId, "CustomerProfiles", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "QuoteRequestItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                QuoteRequestId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductName = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,3)", nullable: false),
                Unit = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_QuoteRequestItems", x => x.Id);
                table.ForeignKey("FK_QuoteRequestItems_QuoteRequests_QuoteRequestId", x => x.QuoteRequestId, "QuoteRequests", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_CustomerProfiles_UserId", "CustomerProfiles", "UserId", unique: true);
        migrationBuilder.CreateIndex("IX_QuoteRequests_CustomerId", "QuoteRequests", "CustomerId");
        migrationBuilder.CreateIndex("IX_QuoteRequests_Status", "QuoteRequests", "Status");
        migrationBuilder.CreateIndex("IX_QuoteRequestItems_QuoteRequestId", "QuoteRequestItems", "QuoteRequestId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("QuoteRequestItems");
        migrationBuilder.DropTable("QuoteRequests");
        migrationBuilder.DropTable("CustomerProfiles");
    }
}
