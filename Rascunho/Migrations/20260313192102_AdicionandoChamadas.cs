using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Rascunho.Migrations
{
    /// <inheritdoc />
    public partial class AdicionandoChamadas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosPresencas",
                columns: table => new
                {
                    TurmaId = table.Column<int>(type: "integer", nullable: false),
                    AlunoId = table.Column<int>(type: "integer", nullable: false),
                    DataAula = table.Column<DateOnly>(type: "date", nullable: false),
                    Presente = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosPresencas", x => new { x.TurmaId, x.AlunoId, x.DataAula });
                    table.ForeignKey(
                        name: "FK_RegistrosPresencas_Turmas_TurmaId",
                        column: x => x.TurmaId,
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_RegistrosPresencas_Usuarios_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPresencas_AlunoId",
                table: "RegistrosPresencas",
                column: "AlunoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosPresencas");
        }
    }
}
