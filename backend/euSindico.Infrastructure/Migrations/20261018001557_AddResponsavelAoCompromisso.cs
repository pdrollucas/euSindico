using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace euSindico.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddResponsavelAoCompromisso : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "responsavel_usuario_id",
                table: "compromissos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_compromissos_predio_id_responsavel_usuario_id",
                table: "compromissos",
                columns: new[] { "predio_id", "responsavel_usuario_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compromissos_responsavel_usuario_id",
                table: "compromissos",
                column: "responsavel_usuario_id");

            migrationBuilder.AddForeignKey(
                name: "FK_compromissos_usuarios_responsavel_usuario_id",
                table: "compromissos",
                column: "responsavel_usuario_id",
                principalTable: "usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_compromissos_usuarios_responsavel_usuario_id",
                table: "compromissos");

            migrationBuilder.DropIndex(
                name: "IX_compromissos_predio_id_responsavel_usuario_id",
                table: "compromissos");

            migrationBuilder.DropIndex(
                name: "IX_compromissos_responsavel_usuario_id",
                table: "compromissos");

            migrationBuilder.DropColumn(
                name: "responsavel_usuario_id",
                table: "compromissos");
        }
    }
}
