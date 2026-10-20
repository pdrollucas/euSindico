namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada quando o responsável informado para um compromisso não é membro da equipe do
/// prédio de destino (RN20).
/// </summary>
public class ResponsavelInvalidoException() : Exception("O responsável deve ser um usuário vinculado ao prédio.");
