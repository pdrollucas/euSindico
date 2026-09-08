using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace euSindico.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEquipeEAcesso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "convites_funcionario",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    predio_id = table.Column<int>(type: "int", nullable: false),
                    email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    papel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    token_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    criado_por_usuario_id = table.Column<int>(type: "int", nullable: false),
                    criado_em = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expira_em = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    usado_em = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_convites_funcionario", x => x.Id);
                    table.ForeignKey(
                        name: "FK_convites_funcionario_predios_predio_id",
                        column: x => x.predio_id,
                        principalTable: "predios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_convites_funcionario_usuarios_criado_por_usuario_id",
                        column: x => x.criado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "predio_usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    predio_id = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    papel = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    convidado_por_usuario_id = table.Column<int>(type: "int", nullable: true),
                    criado_em = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_predio_usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_predio_usuarios_predios_predio_id",
                        column: x => x.predio_id,
                        principalTable: "predios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_predio_usuarios_usuarios_convidado_por_usuario_id",
                        column: x => x.convidado_por_usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_predio_usuarios_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_convites_funcionario_criado_por_usuario_id",
                table: "convites_funcionario",
                column: "criado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_convites_funcionario_predio_id_email",
                table: "convites_funcionario",
                columns: new[] { "predio_id", "email" });

            migrationBuilder.CreateIndex(
                name: "IX_convites_funcionario_token_hash",
                table: "convites_funcionario",
                column: "token_hash",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_predio_usuarios_convidado_por_usuario_id",
                table: "predio_usuarios",
                column: "convidado_por_usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_predio_usuarios_predio_id_usuario_id",
                table: "predio_usuarios",
                columns: new[] { "predio_id", "usuario_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_predio_usuarios_usuario_id",
                table: "predio_usuarios",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "convites_funcionario");

            migrationBuilder.DropTable(
                name: "predio_usuarios");
        }
    }
}
