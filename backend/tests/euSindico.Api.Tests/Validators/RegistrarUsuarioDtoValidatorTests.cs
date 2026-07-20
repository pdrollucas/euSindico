using euSindico.Api.Validators;
using euSindico.Application.Auth.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

/// <summary>
/// Testa só a composição (o DTO chama o validator certo em cada campo) — a cobertura
/// exaustiva de cada regra já está em NomeValidatorTests, EmailValidatorTests e
/// SenhaForteValidatorTests.
/// </summary>
public class RegistrarUsuarioDtoValidatorTests
{
    private readonly RegistrarUsuarioDtoValidator _sut = new();

    [Fact]
    public void Dto_valido_nao_gera_erro()
    {
        var dto = new RegistrarUsuarioDto("João Silva", "joao@eusindico.com", "Senha@123");

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Nome_invalido_gera_erro()
    {
        var dto = new RegistrarUsuarioDto("<script>alert('Pedro')</script>", "joao@eusindico.com", "Senha@123");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void Email_invalido_gera_erro()
    {
        var dto = new RegistrarUsuarioDto("João Silva", "<script>alert('Pedro')</script>@hot", "Senha@123");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Senha_fraca_gera_erro()
    {
        var dto = new RegistrarUsuarioDto("João Silva", "joao@eusindico.com", "123");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Senha);
    }
}
