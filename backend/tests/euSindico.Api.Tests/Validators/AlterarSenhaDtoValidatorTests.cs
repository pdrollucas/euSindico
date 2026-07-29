using euSindico.Api.Validators;
using euSindico.Application.Auth.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

/// <summary>
/// Testa só a composição da nova senha (cobertura exaustiva em SenhaForteValidatorTests)
/// e as regras próprias deste DTO (senha atual obrigatória, mas sem exigir força dela).
/// </summary>
public class AlterarSenhaDtoValidatorTests
{
    private readonly AlterarSenhaDtoValidator _sut = new();

    [Fact]
    public void Dto_valido_nao_gera_erro()
    {
        var dto = new AlterarSenhaDto("SenhaAtual@1", "SenhaNova@2");

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Senha_atual_vazia_gera_erro()
    {
        var dto = new AlterarSenhaDto(string.Empty, "SenhaNova@2");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.SenhaAtual);
    }

    [Fact]
    public void Senha_atual_fraca_nao_gera_erro_pois_so_a_nova_precisa_ser_forte()
    {
        // A senha atual é só conferida contra o hash existente — não faz sentido
        // exigir que ela atenda às regras atuais de força (pode ser uma senha antiga).
        var dto = new AlterarSenhaDto("123", "SenhaNova@2");

        _sut.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.SenhaAtual);
    }

    [Fact]
    public void Nova_senha_fraca_gera_erro()
    {
        var dto = new AlterarSenhaDto("SenhaAtual@1", "123");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.NovaSenha);
    }
}
