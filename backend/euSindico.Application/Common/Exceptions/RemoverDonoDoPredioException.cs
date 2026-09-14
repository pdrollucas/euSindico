namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Um síndico nunca pode remover o próprio vínculo de dono via este fluxo (RF32) — isso
/// deixaria o prédio sem ninguém para gerenciar a equipe. Excluir o prédio inteiro é um
/// fluxo à parte (<c>DELETE /predios/{id}</c>, ver PREDIOS.md).
/// </summary>
public class RemoverDonoDoPredioException() : Exception("Não é possível remover o acesso do dono do prédio.");
