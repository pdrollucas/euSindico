using euSindico.Api.Validators;
using euSindico.Application.Auth.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class ExcluirContaDtoValidatorTests
{
    private readonly ExcluirContaDtoValidator _sut = new();

    [Fact]
    public void Senha_preenchida_nao_gera_erro()
    {
        _sut.TestValidate(new ExcluirContaDto("Senha@123")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Senha_vazia_gera_erro()
    {
        _sut.TestValidate(new ExcluirContaDto("")).ShouldHaveValidationErrorFor(x => x.Senha);
    }
}
