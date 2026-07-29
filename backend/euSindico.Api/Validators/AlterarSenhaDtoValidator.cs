using euSindico.Application.Auth.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class AlterarSenhaDtoValidator : AbstractValidator<AlterarSenhaDto>
{
    public AlterarSenhaDtoValidator()
    {
        RuleFor(x => x.SenhaAtual)
            .NotEmpty().WithMessage("A senha atual é obrigatória.");

        RuleFor(x => x.NovaSenha)
            .SetValidator(new SenhaForteValidator());
    }
}
