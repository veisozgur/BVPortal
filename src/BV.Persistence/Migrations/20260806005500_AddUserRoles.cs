using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BV.Persistence.Migrations;

public partial class AddUserRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Role",
            table: "Users",
            type: "nvarchar(30)",
            maxLength: 30,
            nullable: false,
            defaultValue: "Customer");

        migrationBuilder.CreateIndex(
            name: "IX_Users_Role",
            table: "Users",
            column: "Role");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_Users_Role",
            table: "Users");

        migrationBuilder.DropColumn(
            name: "Role",
            table: "Users");
    }
}
