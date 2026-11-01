using euSindico.Application.Suporte;
using FluentValidation;

namespace euSindico.Api.Validators;

public class EnviarSuporteDtoValidator : AbstractValidator<EnviarSuporteDto>
{
    public EnviarSuporteDtoValidator()
    {
        // Sem quebra de linha no assunto: ele vira o Subject do e-mail (evita injeção de cabeçalho).
        RuleFor(x => x.Assunto)
            .NotEmpty().WithMessage("O assunto é obrigatório.")
            .MinimumLength(3).WithMessage("O assunto deve ter no mínimo 3 caracteres.")
            .MaximumLength(100).WithMessage("O assunto deve ter no máximo 100 caracteres.")
            .Must(assunto => assunto is null || !assunto.Any(char.IsControl))
            .WithMessage("O assunto não pode conter quebras de linha.");

        RuleFor(x => x.Mensagem)
            .NotEmpty().WithMessage("A mensagem é obrigatória.")
            .MinimumLength(10).WithMessage("A mensagem deve ter no mínimo 10 caracteres.")
            .MaximumLength(2000).WithMessage("A mensagem deve ter no máximo 2000 caracteres.");
    }
}
