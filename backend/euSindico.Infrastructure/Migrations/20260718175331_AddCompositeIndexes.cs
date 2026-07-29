using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace euSindico.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCompositeIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Cria os índices compostos ANTES de derrubar os antigos: o MySQL exige que toda
            // coluna com FK (predio_id, usuario_id) tenha sempre algum índice dando suporte,
            // então não pode haver um instante sem nenhum índice cobrindo essas colunas.
            migrationBuilder.CreateIndex(
                name: "IX_relatorios_predio_id_ano_referencia_mes_referencia",
                table: "relatorios",
                columns: new[] { "predio_id", "ano_referencia", "mes_referencia" });

            migrationBuilder.CreateIndex(
                name: "IX_predios_usuario_id_excluido",
                table: "predios",
                columns: new[] { "usuario_id", "excluido" });

            migrationBuilder.CreateIndex(
                name: "IX_documentos_predio_id_tipo_documento_id",
                table: "documentos",
                columns: new[] { "predio_id", "tipo_documento_id" });

            migrationBuilder.CreateIndex(
                name: "IX_compromissos_predio_id_data_compromisso_horario_compromisso",
                table: "compromissos",
                columns: new[] { "predio_id", "data_compromisso", "horario_compromisso" });

            migrationBuilder.DropIndex(
                name: "IX_relatorios_predio_id",
                table: "relatorios");

            migrationBuilder.DropIndex(
                name: "IX_predios_usuario_id",
                table: "predios");

            migrationBuilder.DropIndex(
                name: "IX_documentos_predio_id",
                table: "documentos");

            migrationBuilder.DropIndex(
                name: "IX_compromissos_predio_id",
                table: "compromissos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Mesma lógica do Up: cria os índices de volta antes de derrubar os compostos,
            // pra nunca deixar as colunas de FK sem nenhum índice de suporte.
            migrationBuilder.CreateIndex(
                name: "IX_relatorios_predio_id",
                table: "relatorios",
                column: "predio_id");

            migrationBuilder.CreateIndex(
                name: "IX_predios_usuario_id",
                table: "predios",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "IX_documentos_predio_id",
                table: "documentos",
                column: "predio_id");

            migrationBuilder.CreateIndex(
                name: "IX_compromissos_predio_id",
                table: "compromissos",
                column: "predio_id");

            migrationBuilder.DropIndex(
                name: "IX_relatorios_predio_id_ano_referencia_mes_referencia",
                table: "relatorios");

            migrationBuilder.DropIndex(
                name: "IX_predios_usuario_id_excluido",
                table: "predios");

            migrationBuilder.DropIndex(
                name: "IX_documentos_predio_id_tipo_documento_id",
                table: "documentos");

            migrationBuilder.DropIndex(
                name: "IX_compromissos_predio_id_data_compromisso_horario_compromisso",
                table: "compromissos");
        }
    }
}
