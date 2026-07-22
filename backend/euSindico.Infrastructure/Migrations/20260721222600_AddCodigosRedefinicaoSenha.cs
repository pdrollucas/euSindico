using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace euSindico.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCodigosRedefinicaoSenha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "codigos_redefinicao_senha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    codigo_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    criado_em = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expira_em = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    usado_em = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_codigos_redefinicao_senha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_codigos_redefinicao_senha_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_codigos_redefinicao_senha_usuario_id_codigo_hash",
                table: "codigos_redefinicao_senha",
                columns: new[] { "usuario_id", "codigo_hash" });

            migrationBuilder.CreateIndex(
                name: "IX_codigos_redefinicao_senha_usuario_id_criado_em",
                table: "codigos_redefinicao_senha",
                columns: new[] { "usuario_id", "criado_em" });

            migrationBuilder.CreateIndex(
                name: "IX_codigos_redefinicao_senha_usuario_id_usado_em",
                table: "codigos_redefinicao_senha",
                columns: new[] { "usuario_id", "usado_em" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "codigos_redefinicao_senha");
        }
    }
}
