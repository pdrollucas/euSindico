namespace euSindico.Application.Auth.Dtos;

public record RedefinirSenhaDto(string Email, string Codigo, string NovaSenha, string ConfirmarSenha);