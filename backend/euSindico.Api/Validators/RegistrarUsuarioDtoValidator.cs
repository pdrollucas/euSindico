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
            .SetValidator(new NomeValidator());

        RuleFor(x => x.Email)
            .SetValidator(new EmailValidator());

        RuleFor(x => x.Senha)
            .SetValidator(new SenhaForteValidator());
    }
}
