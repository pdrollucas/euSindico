using euSindico.Application.Auth.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class AtualizarPerfilDtoValidator : AbstractValidator<AtualizarPerfilDto>
{
    public AtualizarPerfilDtoValidator()
    {
        RuleFor(x => x.Nome)
            .SetValidator(new NomeValidator());
    }
}
