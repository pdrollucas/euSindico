using euSindico.Api.Validators;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class EnderecoValidatorTests
{
    private readonly EnderecoValidator _sut = new();

    [Theory]
    [InlineData("Rua X, 100")]
    [InlineData("Av. Brasil, Nº 500, Apto 12")]
    [InlineData("Rua das Flores, 9 - Bairro Centro")]
    public void Endereco_valido_nao_gera_erro(string endereco) =>
        _sut.TestValidate(endereco).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Endereco_vazio_gera_erro() =>
        _sut.TestValidate(string.Empty).ShouldHaveValidationErrorFor(s => s);

    [Fact]
    public void Endereco_maior_que_255_caracteres_gera_erro() =>
        _sut.TestValidate(new string('a', 256)).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O endereço deve ter no máximo 255 caracteres.");

    [Theory]
    [InlineData("<script>alert('Pedro')</script>")]
    [InlineData("Rua X<img src=x onerror=alert(1)>")]
    [InlineData("Rua X; DROP TABLE predios;")]
    public void Endereco_com_caracteres_nao_permitidos_gera_erro(string endereco) =>
        _sut.TestValidate(endereco).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O endereço contém caracteres não permitidos.");
}
