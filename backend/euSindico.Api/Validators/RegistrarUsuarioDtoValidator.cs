using euSindico.Application.Auth.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

/// <summary>
/// Valida o cadastro de usuário conforme RNF04 (força da senha) e RNF05 (formato de e-mail).
/// </summary>
public class RegistrarUsuarioDtoValidator : AbstractValidator<RegistrarUsuarioDto>
{
    public RegistrarUsuarioDtoValidator()
    {
        RuleFor(x => x.Nome)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(150);

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .EmailAddress().WithMessage("Informe um e-mail em formato válido.")
            .MaximumLength(150);

        RuleFor(x => x.Senha)
            .SetValidator(new SenhaForteValidator());
    }
}
