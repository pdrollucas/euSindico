using euSindico.Api.Validators;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class PrestadorServicoValidatorTests
{
    private readonly PrestadorServicoValidator _sut = new();

    [Theory]
    [InlineData("Elevadores Atlas Ltda.")]
    [InlineData("J&J Pintura")]
    [InlineData("Maria - Jardinagem")]
    [InlineData("Construtora São José S/A")]
    public void Nome_valido_nao_gera_erro(string nome) =>
        _sut.TestValidate(nome).ShouldNotHaveAnyValidationErrors();

    // Null não passa por aqui: o FluentValidation não executa o validador filho de uma propriedade nula
    // (coberto em CompromissoFormDtoValidatorTests); vazio/espaços chegam e também são aceitos.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Nome_ausente_e_permitido_por_ser_opcional(string nome) =>
        _sut.TestValidate(nome).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Nome_maior_que_150_caracteres_gera_erro() =>
        _sut.TestValidate(new string('a', 151)).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O nome do prestador deve ter no máximo 150 caracteres.");

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("Atlas<img src=x>")]
    [InlineData("Atlas \"Ltda\"")]
    public void Nome_com_caracteres_nao_permitidos_gera_erro(string nome) =>
        _sut.TestValidate(nome).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O nome do prestador contém caracteres não permitidos.");
}
