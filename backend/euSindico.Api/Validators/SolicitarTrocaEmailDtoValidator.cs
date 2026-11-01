using euSindico.Application.Auth.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class SolicitarTrocaEmailDtoValidator : AbstractValidator<SolicitarTrocaEmailDto>
{
    public SolicitarTrocaEmailDtoValidator()
    {
        RuleFor(x => x.NovoEmail)
            .SetValidator(new EmailValidator());

        RuleFor(x => x.SenhaAtual)
            .NotEmpty().WithMessage("A senha atual é obrigatória.");
    }
}
