using euSindico.Api.Validators;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class NomeValidatorTests
{
    private readonly NomeValidator _sut = new();

    [Theory]
    [InlineData("João Silva")]
    [InlineData("Mary O'Brien")]
    [InlineData("José D'Ávila-Souza")]
    [InlineData("Ana")]
    public void Nome_valido_nao_gera_erro(string nome) =>
        _sut.TestValidate(nome).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Nome_vazio_gera_erro() =>
        _sut.TestValidate(string.Empty).ShouldHaveValidationErrorFor(s => s);

    [Fact]
    public void Nome_maior_que_150_caracteres_gera_erro() =>
        _sut.TestValidate(new string('a', 151)).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O nome deve ter no máximo 150 caracteres.");

    [Theory]
    [InlineData("<script>alert('Pedro')</script>")]
    [InlineData("João<img src=x onerror=alert(1)>")]
    [InlineData("João123")]
    [InlineData("João; DROP TABLE usuarios;")]
    [InlineData("João@Silva")]
    public void Nome_com_caracteres_nao_permitidos_gera_erro(string nome) =>
        _sut.TestValidate(nome).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O nome deve conter apenas letras, espaços, hífen e apóstrofo.");
}
