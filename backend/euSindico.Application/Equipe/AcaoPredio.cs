namespace euSindico.Application.Equipe;

/// <summary>
/// Ações que dependem do papel de um usuário num prédio (RF33), verificadas por
/// <see cref="AutorizacaoPredioService"/>. Cresce conforme os módulos de Compromissos,
/// Planejamentos, Documentos e Relatórios forem implementados — ver a matriz de permissões
/// em EQUIPE.md.
/// </summary>
public enum AcaoPredio
{
    GerenciarEquipe,
    VisualizarPredio,
    GerenciarPredio,

    /// <summary>
    /// Qualquer papel vinculado acessa compromissos do prédio; o que cada um pode fazer com um
    /// compromisso específico (Colaborador só os próprios, RN21) é decidido pelo CompromissoService.
    /// </summary>
    GerenciarCompromissos,
}
