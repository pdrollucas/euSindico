using euSindico.Api.Validators;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class PredioNomeValidatorTests
{
    private readonly PredioNomeValidator _sut = new();

    [Theory]
    [InlineData("Edifício Aurora")]
    [InlineData("Bloco A - Torre 2")]
    [InlineData("Residencial 9 de Julho")]
    [InlineData("Edifício Solar II")]
    [InlineData("Condomínio São José, Nº 2")]
    public void Nome_valido_nao_gera_erro(string nome) =>
        _sut.TestValidate(nome).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Nome_vazio_gera_erro() =>
        _sut.TestValidate(string.Empty).ShouldHaveValidationErrorFor(s => s);

    [Fact]
    public void Nome_maior_que_150_caracteres_gera_erro() =>
        _sut.TestValidate(new string('a', 151)).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O nome do prédio deve ter no máximo 150 caracteres.");

    [Theory]
    [InlineData("<script>alert('Pedro')</script>")]
    [InlineData("Edifício<img src=x onerror=alert(1)>")]
    [InlineData("Edifício; DROP TABLE predios;")]
    public void Nome_com_caracteres_nao_permitidos_gera_erro(string nome) =>
        _sut.TestValidate(nome).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O nome do prédio contém caracteres não permitidos.");
}
