using euSindico.Application.Auth.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class EsqueciSenhaDtoValidator : AbstractValidator<EsqueciSenhaDto>
{
    public EsqueciSenhaDtoValidator()
    {
        RuleFor(x => x.Email)
            .SetValidator(new EmailValidator());
    }
}