using System.Text.RegularExpressions;
using FluentValidation;

namespace euSindico.Api.Validators;

/// <summary>
/// Nome de pessoa: só letras (com acentuação), espaços, hífen e apóstrofo. Bloqueia,
/// como efeito colateral, qualquer tentativa de injetar HTML/script nesse campo — mas o
/// motivo primário é que isso simplesmente não é um nome válido, não é "sanitização".
/// </summary>
public partial class NomeValidator : AbstractValidator<string>
{
    public NomeValidator()
    {
        RuleFor(nome => nome)
            .NotEmpty().WithMessage("O nome é obrigatório.")
            .MaximumLength(150).WithMessage("O nome deve ter no máximo 150 caracteres.")
            .Matches(NomeRegex()).WithMessage("O nome deve conter apenas letras, espaços, hífen e apóstrofo.");
    }

    [GeneratedRegex(@"^[\p{L}\s'-]+$")]
    private static partial Regex NomeRegex();
}
