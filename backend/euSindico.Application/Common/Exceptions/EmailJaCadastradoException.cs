namespace euSindico.Application.Common.Exceptions;

public class EmailJaCadastradoException(string email) : Exception($"O e-mail '{email}' já está cadastrado.");
