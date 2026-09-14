using euSindico.Api.Validators;
using euSindico.Application.Equipe.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class AceitarConviteDtoValidatorTests
{
    private readonly AceitarConviteDtoValidator _sut = new();

    [Fact]
    public void Nome_e_senha_nulos_nao_geram_erro()
    {
        var dto = new AceitarConviteDto(null, null);

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Nome_e_senha_validos_nao_geram_erro()
    {
        var dto = new AceitarConviteDto("Ana Souza", "Senha@123");

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Nome_invalido_gera_erro_mesmo_com_senha_nula()
    {
        var dto = new AceitarConviteDto("<script>alert('Pedro')</script>", null);

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Nome);
    }

    [Fact]
    public void Senha_fraca_gera_erro_mesmo_com_nome_nulo()
    {
        var dto = new AceitarConviteDto(null, "fraca");

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Senha);
    }
}
