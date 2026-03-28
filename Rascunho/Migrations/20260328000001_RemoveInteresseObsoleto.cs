using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rascunho.Migrations
{
    /// <summary>
    /// BUG-010 (28/03/2026): Remove a tabela Interesses obsoleta.
    /// A entidade Interesse foi substituída funcionalmente pela ListaEspera (Feature #3).
    /// Não há endpoints, serviços ou telas que utilizem a tabela Interesses.
    /// </summary>
    public partial class RemoveInteresseObsoleto : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "Interesses");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Recria a tabela caso a migration seja revertida
            migrationBuilder.CreateTable(
                name: "Interesses",
                columns: table => new
                {
                    TurmaId = table.Column<int>(type: "integer", nullable: false),
                    AlunoId = table.Column<int>(type: "integer", nullable: false),
                    DataRegistro = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interesses", x => new { x.TurmaId, x.AlunoId });
                });
        }
    }
}
