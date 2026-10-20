using euSindico.Application.Compromissos.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class CompromissoFormDtoValidator : AbstractValidator<CompromissoFormDto>
{
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

        RuleFor(x => x.Detalhes)
            .SetValidator(new DetalhesValidator());

        RuleFor(x => x.ResponsavelUsuarioId)
            .GreaterThan(0).WithMessage("O responsável informado é inválido.");
    }
}
