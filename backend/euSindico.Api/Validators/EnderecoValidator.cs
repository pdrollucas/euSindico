using System.Text.RegularExpressions;
using FluentValidation;

namespace euSindico.Api.Validators;

/// <summary>
/// Endereço de prédio: letras, números, espaço e pontuação comum em endereços (vírgulas,
/// abreviações como "Rua", "Nº", "Apto"). Mesmo raciocínio de <see cref="PredioNomeValidator"/> —
/// allowlist de caracteres, não "sanitização".
/// </summary>
public partial class EnderecoValidator : AbstractValidator<string>
{
    public EnderecoValidator()
    {
        RuleFor(endereco => endereco)
            .NotEmpty().WithMessage("O endereço é obrigatório.")
            .MaximumLength(255).WithMessage("O endereço deve ter no máximo 255 caracteres.")
            .Matches(EnderecoRegex()).WithMessage("O endereço contém caracteres não permitidos.");
    }

    [GeneratedRegex(@"^[\p{L}\p{N}\s.,'\-\/ºª]+$")]
    private static partial Regex EnderecoRegex();
}
