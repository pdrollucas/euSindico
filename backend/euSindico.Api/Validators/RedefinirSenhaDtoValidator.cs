using euSindico.Application.Auth.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class RedefinirSenhaDtoValidator : AbstractValidator<RedefinirSenhaDto>
{
    public RedefinirSenhaDtoValidator()
    {
        RuleFor(x => x.Email)
            .SetValidator(new EmailValidator());

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("O código é obrigatório.")
            .Length(6).WithMessage("O código deve ter 6 caracteres.");

        RuleFor(x => x.NovaSenha)
            .SetValidator(new SenhaForteValidator());

        RuleFor(x => x.ConfirmarSenha)
            .Equal(x => x.NovaSenha).WithMessage("A confirmação de senha não corresponde à nova senha.");
    }
}