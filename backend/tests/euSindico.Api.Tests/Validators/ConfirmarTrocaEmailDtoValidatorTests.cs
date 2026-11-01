using euSindico.Api.Validators;
using euSindico.Application.Auth.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class ConfirmarTrocaEmailDtoValidatorTests
{
    private readonly ConfirmarTrocaEmailDtoValidator _sut = new();

    [Fact]
    public void Codigo_de_6_caracteres_nao_gera_erro()
    {
        _sut.TestValidate(new ConfirmarTrocaEmailDto("ABC234")).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ABC12")]
    [InlineData("ABC2345")]
    public void Codigo_vazio_ou_de_tamanho_errado_gera_erro(string codigo)
    {
        _sut.TestValidate(new ConfirmarTrocaEmailDto(codigo)).ShouldHaveValidationErrorFor(x => x.Codigo);
    }
}
