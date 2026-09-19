using euSindico.Application.Predios.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class PredioFormDtoValidator : AbstractValidator<PredioFormDto>
{
    public PredioFormDtoValidator()
    {
        RuleFor(x => x.Nome)
            .SetValidator(new PredioNomeValidator());

        RuleFor(x => x.Endereco)
            .SetValidator(new EnderecoValidator());
    }
}
