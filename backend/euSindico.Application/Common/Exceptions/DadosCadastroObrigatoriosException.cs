namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada ao aceitar um convite (RF30) cujo e-mail ainda não tem conta, sem que
/// nome/senha tenham sido informados no corpo da requisição.
/// </summary>
public class DadosCadastroObrigatoriosException() : Exception("Nome e senha são obrigatórios para criar sua conta.");
