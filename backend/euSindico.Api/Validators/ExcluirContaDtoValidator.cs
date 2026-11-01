using euSindico.Application.Auth.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class ExcluirContaDtoValidator : AbstractValidator<ExcluirContaDto>
{
    public ExcluirContaDtoValidator()
    {
        RuleFor(x => x.Senha)
            .NotEmpty().WithMessage("A senha é obrigatória.");
    }
}
