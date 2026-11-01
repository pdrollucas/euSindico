namespace euSindico.Application.Documentos;

/// <summary>
/// Onde os arquivos de atas e normas ficam no armazenamento. Concentra o formato da chave para quem
/// grava (<see cref="DocumentoService"/>) e quem apaga por prédio (exclusão de conta) usarem o mesmo prefixo.
/// </summary>
public static class ChaveDocumento
{
    /// <summary>Prefixo de todos os documentos de um prédio; termina em <c>/</c> de propósito.</summary>
    public static string Prefixo(int predioId) => $"documentos/{predioId}/";

    // Só ids, um guid e a extensão já validada (allowlist de RN12) — nunca o nome enviado pelo usuário:
    // sem path traversal e sem colisão.
    public static string Nova(int predioId, string extensao) => $"{Prefixo(predioId)}{Guid.NewGuid():N}{extensao}";
}
