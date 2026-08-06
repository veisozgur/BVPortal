using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BV.Persistence.Migrations;

public partial class AddSchoolSupplySets : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Schools",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                ContactName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                Phone = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                Email = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                Address = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_Schools", x => x.Id));

        migrationBuilder.CreateTable(
            name: "SchoolGrades",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                SortOrder = table.Column<int>(type: "int", nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SchoolGrades", x => x.Id);
                table.ForeignKey("FK_SchoolGrades_Schools_SchoolId", x => x.SchoolId, "Schools", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "SchoolSupplySets",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SchoolGradeId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                AcademicYear = table.Column<int>(type: "int", nullable: false),
                Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SchoolSupplySets", x => x.Id);
                table.ForeignKey("FK_SchoolSupplySets_Schools_SchoolId", x => x.SchoolId, "Schools", "Id", onDelete: ReferentialAction.Restrict);
                table.ForeignKey("FK_SchoolSupplySets_SchoolGrades_SchoolGradeId", x => x.SchoolGradeId, "SchoolGrades", "Id", onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateTable(
            name: "SchoolSupplySetItems",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                SupplySetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                ProductName = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                Quantity = table.Column<decimal>(type: "decimal(18,3)", precision: 18, scale: 3, nullable: false),
                Unit = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_SchoolSupplySetItems", x => x.Id);
                table.ForeignKey("FK_SchoolSupplySetItems_Products_ProductId", x => x.ProductId, "Products", "Id", onDelete: ReferentialAction.SetNull);
                table.ForeignKey("FK_SchoolSupplySetItems_SchoolSupplySets_SupplySetId", x => x.SupplySetId, "SchoolSupplySets", "Id", onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex("IX_Schools_Name", "Schools", "Name");
        migrationBuilder.CreateIndex("IX_Schools_Code", "Schools", "Code", unique: true, filter: "[Code] IS NOT NULL");
        migrationBuilder.CreateIndex("IX_SchoolGrades_SchoolId_Name", "SchoolGrades", new[] { "SchoolId", "Name" }, unique: true);
        migrationBuilder.CreateIndex("IX_SchoolGrades_SchoolId_SortOrder", "SchoolGrades", new[] { "SchoolId", "SortOrder" });
        migrationBuilder.CreateIndex("IX_SchoolSupplySets_SchoolGradeId", "SchoolSupplySets", "SchoolGradeId");
        migrationBuilder.CreateIndex("IX_SchoolSupplySets_SchoolId_SchoolGradeId_AcademicYear", "SchoolSupplySets", new[] { "SchoolId", "SchoolGradeId", "AcademicYear" }, unique: true);
        migrationBuilder.CreateIndex("IX_SchoolSupplySetItems_ProductId", "SchoolSupplySetItems", "ProductId");
        migrationBuilder.CreateIndex("IX_SchoolSupplySetItems_SupplySetId", "SchoolSupplySetItems", "SupplySetId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable("SchoolSupplySetItems");
        migrationBuilder.DropTable("SchoolSupplySets");
        migrationBuilder.DropTable("SchoolGrades");
        migrationBuilder.DropTable("Schools");
    }
}
