using System.Text.RegularExpressions;
using FluentValidation;

namespace euSindico.Api.Validators;

/// <summary>
/// Nome do prestador de serviço (opcional — só validado quando preenchido). Aceita nome de empresa
/// ("Elevadores Atlas Ltda.", "J&amp;J Pintura", "Maria - Jardinagem"). Mesma allowlist de <see cref="TituloValidator"/>.
/// </summary>
public partial class PrestadorServicoValidator : AbstractValidator<string?>
{
    public PrestadorServicoValidator()
    {
        When(nome => !string.IsNullOrWhiteSpace(nome), () =>
        {
            RuleFor(nome => nome!)
                .MaximumLength(150).WithMessage("O nome do prestador deve ter no máximo 150 caracteres.")
                .Matches(PrestadorRegex()).WithMessage("O nome do prestador contém caracteres não permitidos.");
        });
    }

    [GeneratedRegex(@"^[\p{L}\p{N}\s.,:;'\-()/ºª&%#!?+@$°]+$")]
    private static partial Regex PrestadorRegex();
}
