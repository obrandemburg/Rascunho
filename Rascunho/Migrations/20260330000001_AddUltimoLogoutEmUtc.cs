using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rascunho.Migrations
{
    /// <summary>
    /// FIX (30/03/2026): Adiciona coluna UltimoLogoutEmUtc à tabela Usuarios.
    /// Usada para invalidação de tokens JWT no servidor no momento do logout.
    /// Quando o usuário efetua logout, este campo é atualizado com DateTime.UtcNow.
    /// O middleware de autenticação rejeita tokens com "iat" anterior a este valor.
    /// </summary>
    public partial class AddUltimoLogoutEmUtc : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UltimoLogoutEmUtc",
                table: "Usuarios",
                type: "timestamp with time zone",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UltimoLogoutEmUtc",
                table: "Usuarios");
        }
    }
}
