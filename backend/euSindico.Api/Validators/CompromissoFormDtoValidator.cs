using euSindico.Application.Compromissos.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class CompromissoFormDtoValidator : AbstractValidator<CompromissoFormDto>
{
    // decimal(10,2) no banco: 8 dígitos inteiros + 2 casas.
    public const decimal OrcamentoMaximo = 99_999_999.99m;

    public CompromissoFormDtoValidator()
    {
        // Anulável no DTO para "predioId": null chegar aqui (formato padrão de erro do projeto) em vez de
        // falhar na desserialização, antes do validador.
        RuleFor(x => x.PredioId)
            .NotNull().WithMessage("O prédio é obrigatório.")
            .GreaterThan(0).WithMessage("O prédio é obrigatório.");

        RuleFor(x => x.Titulo)
            .SetValidator(new TituloValidator());

        // RN10: data e horário são obrigatórios. Datas passadas são permitidas (registro de atividade já realizada).
        RuleFor(x => x.DataCompromisso)
            .NotNull().WithMessage("A data do compromisso é obrigatória.");

        RuleFor(x => x.HorarioCompromisso)
            .NotNull().WithMessage("O horário do compromisso é obrigatório.");

        RuleFor(x => x.NomePrestadorServico)
            .SetValidator(new PrestadorServicoValidator());

        // O orçamento é opcional e aceita zero. Mais de 2 casas decimais seriam arredondadas em
        // silêncio pelo banco.
        RuleFor(x => x.Orcamento)
            .GreaterThanOrEqualTo(0m).WithMessage("O orçamento não pode ser negativo.")
            .LessThanOrEqualTo(OrcamentoMaximo).WithMessage("O orçamento excede o valor máximo permitido.")
            .Must(valor => decimal.Round(valor!.Value, 2) == valor.Value)
            .WithMessage("O orçamento deve ter no máximo 2 casas decimais.")
            .When(x => x.Orcamento is not null);

        RuleFor(x => x.Detalhes)
            .SetValidator(new DetalhesValidator());

        RuleFor(x => x.ResponsavelUsuarioId)
            .GreaterThan(0).WithMessage("O responsável informado é inválido.");
    }
}
