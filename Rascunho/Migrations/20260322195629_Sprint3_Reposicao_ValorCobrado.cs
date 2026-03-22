using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Rascunho.Migrations
{
    /// <inheritdoc />
    public partial class Sprint3_Reposicao_ValorCobrado : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "ValorCobrado",
                table: "AulasParticulares",
                type: "numeric(10,2)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.CreateTable(
                name: "Reposicoes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AlunoId = table.Column<int>(type: "integer", nullable: false),
                    TurmaOrigemId = table.Column<int>(type: "integer", nullable: false),
                    DataFalta = table.Column<DateOnly>(type: "date", nullable: false),
                    TurmaDestinoId = table.Column<int>(type: "integer", nullable: false),
                    DataReposicaoAgendada = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Status = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    DataSolicitacao = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Reposicoes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Reposicoes_Turmas_TurmaDestinoId",
                        column: x => x.TurmaDestinoId,
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reposicoes_Turmas_TurmaOrigemId",
                        column: x => x.TurmaOrigemId,
                        principalTable: "Turmas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Reposicoes_Usuarios_AlunoId",
                        column: x => x.AlunoId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Reposicoes_AlunoId_TurmaOrigemId_DataFalta_Status",
                table: "Reposicoes",
                columns: new[] { "AlunoId", "TurmaOrigemId", "DataFalta", "Status" },
                unique: true,
                filter: "\"Status\" = 'Agendada'");

            migrationBuilder.CreateIndex(
                name: "IX_Reposicoes_TurmaDestinoId",
                table: "Reposicoes",
                column: "TurmaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Reposicoes_TurmaOrigemId",
                table: "Reposicoes",
                column: "TurmaOrigemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Reposicoes");

            migrationBuilder.DropColumn(
                name: "ValorCobrado",
                table: "AulasParticulares");
        }
    }
}
