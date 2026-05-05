// ADD new migration file:
// EquaMeridian.Infrastructure/Migrations/AddLastPasswordChangeAt.cs
//
// Run: dotnet ef migrations add AddLastPasswordChangeAt --project EquaMeridian.Infrastructure --startup-project EquaMeridian

using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EquaMeridian.Infrastructure.Migrations
{
    public partial class AddLastPasswordChangeAt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "LastPasswordChangeAt",
                table: "Users",
                type: "datetime2",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastPasswordChangeAt",
                table: "Users");
        }
    }
}