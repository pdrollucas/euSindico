using euSindico.Application.Auth.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class VerificarCodigoDtoValidator : AbstractValidator<VerificarCodigoDto>
{
    public VerificarCodigoDtoValidator()
    {
        RuleFor(x => x.Email)
            .SetValidator(new EmailValidator());

        RuleFor(x => x.Codigo)
            .NotEmpty().WithMessage("O código é obrigatório.")
            .Length(6).WithMessage("O código deve ter 6 caracteres.");
    }
}