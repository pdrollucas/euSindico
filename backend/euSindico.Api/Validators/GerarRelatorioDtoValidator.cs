using euSindico.Application.Relatorios.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

public class GerarRelatorioDtoValidator : AbstractValidator<GerarRelatorioDto>
{
    public const int AnoMinimo = 2000;

    public GerarRelatorioDtoValidator()
    {
        RuleFor(x => x.Mes)
            .NotNull().WithMessage("O mês é obrigatório.")
            .InclusiveBetween(1, 12).WithMessage("O mês deve estar entre 1 e 12.");

        // Mês futuro depende do fuso da aplicação e fica no RelatorioService.
        RuleFor(x => x.Ano)
            .NotNull().WithMessage("O ano é obrigatório.")
            .InclusiveBetween(AnoMinimo, 9999).WithMessage($"O ano deve ser {AnoMinimo} ou posterior.");
    }
}
