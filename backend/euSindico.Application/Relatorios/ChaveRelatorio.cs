namespace euSindico.Application.Relatorios;

/// <summary>
/// Onde os PDFs dos relatórios ficam no armazenamento. Concentra o formato da chave para quem grava
/// (<see cref="RelatorioService"/>) e quem apaga por prédio (exclusão de conta) usarem o mesmo prefixo.
/// </summary>
public static class ChaveRelatorio
{
    /// <summary>Prefixo de todos os relatórios de um prédio; termina em <c>/</c> de propósito.</summary>
    public static string Prefixo(int predioId) => $"relatorios/{predioId}/";

    // Só ids e um guid — nunca dado do usuário: sem path traversal e sem colisão.
    public static string Nova(int predioId) => $"{Prefixo(predioId)}{Guid.NewGuid():N}.pdf";
}
