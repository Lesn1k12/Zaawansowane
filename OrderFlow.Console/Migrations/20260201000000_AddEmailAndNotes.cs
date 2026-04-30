using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderFlow.Console.Migrations;

/// <inheritdoc />
public partial class AddEmailAndNotes : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Email",
            table: "Customers",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<string>(
            name: "Notes",
            table: "Orders",
            type: "TEXT",
            nullable: true);

        migrationBuilder.AddColumn<int>(
            name: "Stock",
            table: "Products",
            type: "INTEGER",
            nullable: false,
            defaultValue: 0);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(name: "Email", table: "Customers");
        migrationBuilder.DropColumn(name: "Notes",  table: "Orders");
        migrationBuilder.DropColumn(name: "Stock",  table: "Products");
    }
}
