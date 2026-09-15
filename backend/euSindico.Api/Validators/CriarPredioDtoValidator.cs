using euSindico.Application.Predios.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class CriarPredioDtoValidator : AbstractValidator<CriarPredioDto>
{
    public CriarPredioDtoValidator()
    {
        RuleFor(x => x.Nome)
            .SetValidator(new PredioNomeValidator());

        RuleFor(x => x.Endereco)
            .SetValidator(new EnderecoValidator());
    }
}
