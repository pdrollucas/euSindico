using System.Text.RegularExpressions;
using FluentValidation;

namespace euSindico.Api.Validators;

/// <summary>
/// Título curto de registro operacional (compromisso e, no futuro, planejamento): letras, números,
/// espaço e a pontuação comum de uma anotação ("Reunião com a síndica - 10:00", "Revisão (elevador) nº 2").
/// Allowlist como defesa em profundidade contra XSS (SECURITY.md, seção 3) — "&lt;", "&gt;", aspas,
/// crases e chaves ficam de fora.
/// </summary>
public partial class TituloValidator : AbstractValidator<string>
{
    public TituloValidator()
    {
        RuleFor(titulo => titulo)
            .NotEmpty().WithMessage("O título é obrigatório.")
            .MaximumLength(150).WithMessage("O título deve ter no máximo 150 caracteres.")
            .Matches(TituloRegex()).WithMessage("O título contém caracteres não permitidos.");
    }

    [GeneratedRegex(@"^[\p{L}\p{N}\s.,:;'\-()/ºª&%#!?+@$°]+$")]
    private static partial Regex TituloRegex();
}
