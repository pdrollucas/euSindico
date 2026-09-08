namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada ao convidar um e-mail que já é membro ativo do prédio ou que já tem um convite
/// pendente (não expirado, não usado) para o mesmo prédio (RN17). Diferente do login/RF06-A,
/// não há preocupação de anti-enumeração aqui — quem convida já é o Síndico autenticado do
/// prédio, escolhendo um e-mail que ele mesmo conhece.
/// </summary>
public class EmailJaVinculadoException(string email)
    : Exception($"O e-mail '{email}' já está vinculado a este prédio ou possui um convite pendente.");
