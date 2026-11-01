namespace euSindico.Application.Common.Exceptions;

/// <summary>Lançada quando o prédio já tem o máximo de normas guardadas (RN40).</summary>
public class LimiteNormasAtingidoException(int limite)
    : Exception($"O limite de {limite} normas foi atingido para este prédio. Exclua uma norma existente para enviar outra.");
