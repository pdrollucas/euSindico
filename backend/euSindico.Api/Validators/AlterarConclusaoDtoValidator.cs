using euSindico.Application.Compromissos.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class AlterarConclusaoDtoValidator : AbstractValidator<AlterarConclusaoDto>
{
    public AlterarConclusaoDtoValidator()
    {
        RuleFor(x => x.Concluido)
            .NotNull().WithMessage("Informe se o compromisso está concluído.");
    }
}
