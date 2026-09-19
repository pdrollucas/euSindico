using euSindico.Api.Validators;
using euSindico.Application.Predios.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class PredioFormDtoValidatorTests
{
    private readonly PredioFormDtoValidator _sut = new();

    [Fact]
    public void Dto_valido_nao_gera_erro()
    {
        var dto = new PredioFormDto("Edifício Aurora", "Rua X, 100");

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Nome_invalido_gera_erro_no_campo_nome()
    {
        var dto = new PredioFormDto("<script>alert(1)</script>", "Rua X, 100");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void Endereco_invalido_gera_erro_no_campo_endereco()
    {
        var dto = new PredioFormDto("Edifício Aurora", "<script>alert(1)</script>");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Endereco);
    }
}
