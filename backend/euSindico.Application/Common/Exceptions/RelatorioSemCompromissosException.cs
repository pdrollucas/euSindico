namespace euSindico.Application.Common.Exceptions;

/// <summary>Lançada quando o mês pedido não tem compromissos concluídos (RN06) — nenhum relatório é gerado.</summary>
public class RelatorioSemCompromissosException()
    : Exception("Não há compromissos concluídos no período selecionado.");
