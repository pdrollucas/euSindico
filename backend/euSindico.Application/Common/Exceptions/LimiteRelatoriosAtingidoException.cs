namespace euSindico.Application.Common.Exceptions;

/// <summary>Lançada quando o prédio já tem o máximo de relatórios do mês de referência (RN36).</summary>
public class LimiteRelatoriosAtingidoException(int limite)
    : Exception($"O limite de {limite} relatórios por mês foi atingido para este prédio e período. Exclua um relatório existente para gerar outro.");
