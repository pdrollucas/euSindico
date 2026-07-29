using euSindico.Api.Validators;
using euSindico.Application.Auth.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

/// <summary>
/// Testa só a composição do Email/NovaSenha (cobertura exaustiva em EmailValidatorTests e
/// SenhaForteValidatorTests) e as regras próprias deste DTO (código e confirmação de senha).
/// </summary>
public class RedefinirSenhaDtoValidatorTests
{
    private readonly RedefinirSenhaDtoValidator _sut = new();

    [Fact]
    public void Dto_valido_nao_gera_erro()
    {
        var dto = new RedefinirSenhaDto("joao@eusindico.com", "AB12CD", "SenhaNova@2", "SenhaNova@2");

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Codigo_com_tamanho_diferente_de_seis_gera_erro()
    {
        var dto = new RedefinirSenhaDto("joao@eusindico.com", "AB12", "SenhaNova@2", "SenhaNova@2");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Codigo);
    }

    [Fact]
    public void Nova_senha_fraca_gera_erro()
    {
        var dto = new RedefinirSenhaDto("joao@eusindico.com", "AB12CD", "123", "123");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.NovaSenha);
    }

    [Fact]
    public void Confirmar_senha_diferente_da_nova_senha_gera_erro()
    {
        var dto = new RedefinirSenhaDto("joao@eusindico.com", "AB12CD", "SenhaNova@2", "SenhaDiferente@2");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.ConfirmarSenha);
    }
}