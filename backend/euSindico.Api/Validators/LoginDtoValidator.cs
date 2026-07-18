using euSindico.Application.Auth.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

/// <summary>
/// Só valida presença/formato — não valida força de senha aqui (não é cadastro,
/// e revelar regras de senha numa tela de login não ajuda em nada).
/// </summary>
public class LoginDtoValidator : AbstractValidator<LoginDto>
{
    public LoginDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("Informe um e-mail em formato válido.");

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.");
    }
}
