using euSindico.Api.Validators;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class EmailValidatorTests
{
    private readonly EmailValidator _sut = new();

    [Theory]
    [InlineData("joao@eusindico.com")]
    [InlineData("joao.silva+teste@eusindico.com.br")]
    [InlineData("j@a.co")]
    public void Email_valido_nao_gera_erro(string email) =>
        _sut.TestValidate(email).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Email_vazio_gera_erro() =>
        _sut.TestValidate(string.Empty).ShouldHaveValidationErrorFor(s => s);

    [Fact]
    public void Email_maior_que_150_caracteres_gera_erro()
    {
        var emailGigante = new string('a', 146) + "@a.co"; // 151 caracteres no total

        _sut.TestValidate(emailGigante).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O e-mail deve ter no máximo 150 caracteres.");
    }

    [Theory]
    [InlineData("<script>alert('Pedro')</script>@hot")] // parte local com caracteres não permitidos
    [InlineData("joao@hot")] // domínio sem ponto/TLD
    [InlineData("joao@@eusindico.com")] // "@" duplicado
    [InlineData("joao eusindico.com")] // sem "@"
    [InlineData("joao@eusindico com")] // espaço no domínio
    public void Email_com_formato_invalido_gera_erro(string email) =>
        _sut.TestValidate(email).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("Informe um e-mail em formato válido.");
}
