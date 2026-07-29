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
            .SetValidator(new EmailValidator());

        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.");
    }
}
