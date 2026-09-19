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
}
