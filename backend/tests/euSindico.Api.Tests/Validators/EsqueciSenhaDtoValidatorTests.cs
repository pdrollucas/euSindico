using euSindico.Api.Validators;
using euSindico.Application.Auth.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

/// <summary>
/// Testa só a composição do Email (cobertura exaustiva em EmailValidatorTests) —
/// esse DTO não tem nenhuma regra própria além disso.
/// </summary>
public class EsqueciSenhaDtoValidatorTests
{
    private readonly EsqueciSenhaDtoValidator _sut = new();

    [Fact]
    public void Dto_valido_nao_gera_erro()
    {
        var dto = new EsqueciSenhaDto("joao@eusindico.com");

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Email_invalido_gera_erro()
    {
        var dto = new EsqueciSenhaDto("<script>alert('Pedro')</script>@hot");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }
}