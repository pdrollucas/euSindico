namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada ao criar um prédio cujo Nome + Endereço já pertence a outro prédio ativo do
/// mesmo usuário (regra não prevista no RFC, ver PREDIOS.md). Nome sozinho pode repetir —
/// só a combinação exata dos dois campos é bloqueada.
/// </summary>
public class PredioDuplicadoException() : Exception("Já existe um prédio ativo com esse nome e endereço.");
