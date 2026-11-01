using euSindico.Api.Validators;
using euSindico.Application.Auth.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class SolicitarTrocaEmailDtoValidatorTests
{
    private readonly SolicitarTrocaEmailDtoValidator _sut = new();

    [Fact]
    public void Dto_valido_nao_gera_erro()
    {
        _sut.TestValidate(new SolicitarTrocaEmailDto("novo@eusindico.com", "Senha@123")).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Email_invalido_gera_erro()
    {
        _sut.TestValidate(new SolicitarTrocaEmailDto("invalido", "Senha@123")).ShouldHaveValidationErrorFor(x => x.NovoEmail);
    }

    [Fact]
    public void Senha_vazia_gera_erro()
    {
        _sut.TestValidate(new SolicitarTrocaEmailDto("novo@eusindico.com", "")).ShouldHaveValidationErrorFor(x => x.SenhaAtual);
    }
}
