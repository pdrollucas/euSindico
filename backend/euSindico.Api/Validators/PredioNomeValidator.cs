using System.Text.RegularExpressions;
using FluentValidation;

namespace euSindico.Api.Validators;

/// <summary>
/// Nome de prédio: letras, números, espaço e pontuação comum em nomes de edifício
/// ("Edifício Solar II", "Bloco A - Torre 2", "Residencial 9 de Julho"). Diferente de
/// <see cref="NomeValidator"/> (nome de pessoa, só letras) — nomes de prédio legitimamente
/// contêm números.
/// </summary>
public partial class PredioNomeValidator : AbstractValidator<string>
{
    public PredioNomeValidator()
    {
        RuleFor(nome => nome)
            .NotEmpty().WithMessage("O nome do prédio é obrigatório.")
            .MaximumLength(150).WithMessage("O nome do prédio deve ter no máximo 150 caracteres.")
            .Matches(PredioNomeRegex()).WithMessage("O nome do prédio contém caracteres não permitidos.");
    }

    [GeneratedRegex(@"^[\p{L}\p{N}\s.,'\-()/ºª]+$")]
    private static partial Regex PredioNomeRegex();
}
