using euSindico.Api.Validators;
using euSindico.Application.Auth.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

/// <summary>
/// Testa só a composição do Email (cobertura exaustiva em EmailValidatorTests) e as
/// regras próprias do login (presença de senha, e a ausência deliberada de checagem
/// de força de senha).
/// </summary>
public class LoginDtoValidatorTests
{
    private readonly LoginDtoValidator _sut = new();

    [Fact]
    public void Dto_valido_nao_gera_erro()
    {
        var dto = new LoginDto("joao@eusindico.com", "qualquer-coisa");

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Email_invalido_gera_erro()
    {
        var dto = new LoginDto("<script>alert('Pedro')</script>@hot", "qualquer-coisa");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Senha_vazia_gera_erro()
    {
        var dto = new LoginDto("joao@eusindico.com", string.Empty);

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Senha);
    }

    [Fact]
    public void Senha_fraca_nao_gera_erro_pois_login_nao_valida_forca()
    {
        // Diferente do cadastro, login não deve rejeitar por força de senha —
        // isso só existiria pra revelar regras de senha numa tela que não deveria.
        var dto = new LoginDto("joao@eusindico.com", "123");

        _sut.TestValidate(dto).ShouldNotHaveValidationErrorFor(x => x.Senha);
    }
}
