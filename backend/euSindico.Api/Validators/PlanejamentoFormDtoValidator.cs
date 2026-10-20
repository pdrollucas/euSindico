using euSindico.Application.Planejamentos.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class PlanejamentoFormDtoValidator : AbstractValidator<PlanejamentoFormDto>
{
    // decimal(10,2) no banco: 8 dígitos inteiros + 2 casas.
    public const decimal OrcamentoMaximo = 99_999_999.99m;

    public PlanejamentoFormDtoValidator()
    {
        RuleFor(x => x.Titulo)
            .SetValidator(new TituloValidator());

        // RN11: a data é opcional. A regra "hoje ou futura" depende do fuso da aplicação e fica no
        // PlanejamentoService.
        // O orçamento aceita zero. Mais de 2 casas decimais seriam arredondadas em silêncio pelo banco.
        RuleFor(x => x.OrcamentoPrevisto)
            .GreaterThanOrEqualTo(0m).WithMessage("O orçamento previsto não pode ser negativo.")
            .LessThanOrEqualTo(OrcamentoMaximo).WithMessage("O orçamento previsto excede o valor máximo permitido.")
            .Must(valor => decimal.Round(valor!.Value, 2) == valor.Value)
            .WithMessage("O orçamento previsto deve ter no máximo 2 casas decimais.")
            .When(x => x.OrcamentoPrevisto is not null);

        RuleFor(x => x.NomePrestadorServico)
            .SetValidator(new PrestadorServicoValidator());

        RuleFor(x => x.Detalhes)
            .SetValidator(new DetalhesValidator());
    }
}
