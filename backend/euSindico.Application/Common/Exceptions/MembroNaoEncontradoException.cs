namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada ao tentar remover (ou operar sobre) um vínculo de equipe que não existe —
/// diferente de <see cref="PredioNaoEncontradoException"/>, aqui quem chama já passou pela
/// checagem de que é Síndico do prédio, então "prédio não encontrado" seria uma mensagem
/// enganosa; o que não existe é o vínculo do usuário alvo.
/// </summary>
public class MembroNaoEncontradoException() : Exception("Este usuário não faz parte da equipe do prédio.");
