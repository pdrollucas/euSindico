using euSindico.Api.Validators;
using euSindico.Application.Auth.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class RefreshTokenDtoValidatorTests
{
    private readonly RefreshTokenDtoValidator _sut = new();

    [Fact]
    public void Dto_valido_nao_gera_erro()
    {
        var dto = new RefreshTokenDto("qualquer-token-opaco");

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void RefreshToken_vazio_gera_erro()
    {
        var dto = new RefreshTokenDto(string.Empty);

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.RefreshToken);
    }
}