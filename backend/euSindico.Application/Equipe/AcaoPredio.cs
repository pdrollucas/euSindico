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

    /// <summary>
    /// Somente leitura da equipe (RF31) — o Gestor precisa dela para escolher o responsável de um
    /// compromisso (RN20). Convidar e remover continuam em <see cref="GerenciarEquipe"/> (RN19).
    /// </summary>
    VisualizarEquipe,
    VisualizarPredio,
    GerenciarPredio,

    /// <summary>
    /// Qualquer papel vinculado acessa compromissos do prédio; o que cada um pode fazer com um
    /// compromisso específico (Colaborador só os próprios, RN21) é decidido pelo CompromissoService.
    /// </summary>
    GerenciarCompromissos,

    /// <summary>Leitura de planejamentos (RF19) — todos os papéis, inclusive o Colaborador.</summary>
    VisualizarPlanejamentos,

    /// <summary>Criar, editar e remover planejamentos (RF18, RF20, RF21) — Síndico e Gestor.</summary>
    GerenciarPlanejamentos,
}
