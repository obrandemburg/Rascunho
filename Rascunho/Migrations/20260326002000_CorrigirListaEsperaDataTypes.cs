using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rascunho.Migrations
{
    /// <inheritdoc />
    public partial class CorrigirListaEsperaDataTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Alterar colunas de timestamp para timestamp with time zone (PostgreSQL)
            migrationBuilder.AlterColumn<DateTime>(
                name: "DataEntrada",
                table: "ListasEspera",
                type: "timestamp with time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DataNotificacao",
                table: "ListasEspera",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DataExpiracao",
                table: "ListasEspera",
                type: "timestamp with time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverter para timestamp without time zone
            migrationBuilder.AlterColumn<DateTime>(
                name: "DataEntrada",
                table: "ListasEspera",
                type: "timestamp without time zone",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone");

            migrationBuilder.AlterColumn<DateTime>(
                name: "DataNotificacao",
                table: "ListasEspera",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);

            migrationBuilder.AlterColumn<DateTime>(
                name: "DataExpiracao",
                table: "ListasEspera",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp with time zone",
                oldNullable: true);
        }
    }
}
