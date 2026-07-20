using euSindico.Api.Validators;
using euSindico.Application.Auth.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

/// <summary>
/// Testa só a composição (o DTO chama o validator certo em cada campo) — a cobertura
/// exaustiva de cada regra já está em NomeValidatorTests e EmailValidatorTests.
/// </summary>
public class AtualizarPerfilDtoValidatorTests
{
    private readonly AtualizarPerfilDtoValidator _sut = new();

    [Fact]
    public void Dto_valido_nao_gera_erro()
    {
        var dto = new AtualizarPerfilDto("João Silva", "joao@eusindico.com");

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Nome_invalido_gera_erro()
    {
        var dto = new AtualizarPerfilDto("<script>alert('Pedro')</script>", "joao@eusindico.com");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void Email_invalido_gera_erro()
    {
        var dto = new AtualizarPerfilDto("João Silva", "<script>alert('Pedro')</script>@hot");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }
}
