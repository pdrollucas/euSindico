using euSindico.Application.Equipe.Dtos;
using FluentValidation;

namespace euSindico.Api.Validators;

/// <summary>
/// Nome/Senha só existem no corpo quando o e-mail do convite ainda não tem conta — por
/// isso a validação de formato só roda quando o campo foi de fato enviado; a obrigatoriedade
/// condicional (exigidos quando não há conta) é responsabilidade do EquipeService, que é
/// quem sabe se a conta já existe (DadosCadastroObrigatoriosException).
/// </summary>
public class AceitarConviteDtoValidator : AbstractValidator<AceitarConviteDto>
{
    public AceitarConviteDtoValidator()
    {
        // O null-forgiving (!) é seguro aqui: o .When() abaixo só executa a regra quando o
        // campo não é nulo — a nulidade em si (obrigatoriedade condicional) é decidida pelo
        // EquipeService, que sabe se o e-mail do convite já tem conta (DadosCadastroObrigatoriosException).
        RuleFor(x => x.Nome!)
            .SetValidator(new NomeValidator())
            .When(x => x.Nome is not null);

        RuleFor(x => x.Senha!)
            .SetValidator(new SenhaForteValidator())
            .When(x => x.Senha is not null);
    }
}
