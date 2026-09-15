namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada ao tentar criar um prédio quando o usuário já possui 20 prédios ativos (regra
/// não prevista no RFC, ver PREDIOS.md). Prédios excluídos (soft delete) não contam.
/// </summary>
public class PredioLimiteAtingidoException() : Exception("Limite de 20 prédios ativos atingido.");
