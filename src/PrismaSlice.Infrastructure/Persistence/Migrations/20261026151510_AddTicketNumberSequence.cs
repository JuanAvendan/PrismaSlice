using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace PrismaSlice.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTicketNumberSequence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateSequence<int>(
                name: "TicketNumbers");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Tickets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                defaultValueSql: "'TCK-' || lpad(nextval('\"TicketNumbers\"')::text, 5, '0')",
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropSequence(
                name: "TicketNumbers");

            migrationBuilder.AlterColumn<string>(
                name: "Code",
                table: "Tickets",
                type: "character varying(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(20)",
                oldMaxLength: 20,
                oldDefaultValueSql: "'TCK-' || lpad(nextval('\"TicketNumbers\"')::text, 5, '0')");
        }
    }
}
