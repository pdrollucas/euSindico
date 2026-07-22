using euSindico.Api.Validators;
using euSindico.Application.Auth.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class VerificarCodigoDtoValidatorTests
{
    private readonly VerificarCodigoDtoValidator _sut = new();

    [Fact]
    public void Dto_valido_nao_gera_erro()
    {
        var dto = new VerificarCodigoDto("joao@eusindico.com", "AB12CD");

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Email_invalido_gera_erro()
    {
        var dto = new VerificarCodigoDto("<script>alert('Pedro')</script>@hot", "AB12CD");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Codigo_vazio_gera_erro()
    {
        var dto = new VerificarCodigoDto("joao@eusindico.com", string.Empty);

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Codigo);
    }

    [Fact]
    public void Codigo_com_tamanho_diferente_de_seis_gera_erro()
    {
        var dto = new VerificarCodigoDto("joao@eusindico.com", "AB12");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Codigo);
    }
}