using System.Text.RegularExpressions;
using FluentValidation;

namespace euSindico.Api.Validators;

/// <summary>
/// Mais rigoroso que o `.EmailAddress()` padrão do FluentValidation (modo compatível com
/// ASP.NET Core), que só exige um único "@" com algo não-vazio dos dois lados — aceitando
/// coisas como "&lt;script&gt;...&lt;/script&gt;@hot". Aqui, a parte local só aceita os
/// caracteres realmente válidos em e-mail (RFC 5322/HTML5), e o domínio precisa ter
/// pelo menos um ponto (rejeita domínios sem TLD, tipo "@hot").
/// </summary>
public partial class EmailValidator : AbstractValidator<string>
{
    public EmailValidator()
    {
        RuleFor(email => email)
            .NotEmpty().WithMessage("O e-mail é obrigatório.")
            .MaximumLength(150).WithMessage("O e-mail deve ter no máximo 150 caracteres.")
            .Matches(EmailRegex()).WithMessage("Informe um e-mail em formato válido.");
    }

    [GeneratedRegex(@"^[a-zA-Z0-9.!#$%&'*+/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)+$")]
    private static partial Regex EmailRegex();
}
