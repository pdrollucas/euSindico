using FluentValidation;

namespace euSindico.Api.Validators;

/// <summary>
/// Campo "detalhes" (opcional, texto livre — compromisso e, no futuro, planejamento). Sem allowlist
/// de propósito: uma regex restritiva bloquearia texto legítimo ("R$ 1.200,00", "50%", "e-mail@x.com",
/// quebras de linha). Só limita o tamanho e rejeita "&lt;" e "&gt;", que nunca fazem parte de uma anotação
/// legítima e são a base de qualquer injeção de HTML. A defesa primária continua sendo o escape do Vue.
/// </summary>
public class DetalhesValidator : AbstractValidator<string?>
{
    public const int TamanhoMaximo = 2000;

    public DetalhesValidator()
    {
        When(detalhes => !string.IsNullOrWhiteSpace(detalhes), () =>
        {
            RuleFor(detalhes => detalhes!)
                .MaximumLength(TamanhoMaximo).WithMessage($"Os detalhes devem ter no máximo {TamanhoMaximo} caracteres.")
                .Must(detalhes => !detalhes.Contains('<') && !detalhes.Contains('>'))
                .WithMessage("Os detalhes contêm caracteres não permitidos.");
        });
    }
}
