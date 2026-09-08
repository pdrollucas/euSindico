using euSindico.Application.Equipe.Dtos;
using euSindico.Domain.Enums;
using FluentValidation;

namespace euSindico.Api.Validators;

public class ConvidarFuncionarioDtoValidator : AbstractValidator<ConvidarFuncionarioDto>
{
    public ConvidarFuncionarioDtoValidator()
    {
        RuleFor(x => x.Email)
            .SetValidator(new EmailValidator());

        RuleFor(x => x.Papel)
            .IsInEnum().WithMessage("Perfil de acesso inválido.")
            .NotEqual(PapelPredio.Sindico).WithMessage("Não é possível convidar alguém como Síndico.");
    }
}
