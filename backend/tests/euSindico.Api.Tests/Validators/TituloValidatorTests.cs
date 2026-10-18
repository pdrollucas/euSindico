using euSindico.Api.Validators;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class TituloValidatorTests
{
    private readonly TituloValidator _sut = new();

    [Theory]
    [InlineData("Visita técnica")]
    [InlineData("Reunião com a síndica - 10:00")]
    [InlineData("Revisão (elevador) nº 2")]
    [InlineData("Orçamento R$ 1.200,00 + 50% de entrada")]
    [InlineData("Ligar p/ Maria & João!")]
    public void Titulo_valido_nao_gera_erro(string titulo) =>
        _sut.TestValidate(titulo).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Titulo_vazio_gera_erro() =>
        _sut.TestValidate(string.Empty).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O título é obrigatório.");

    [Fact]
    public void Titulo_maior_que_150_caracteres_gera_erro() =>
        _sut.TestValidate(new string('a', 151)).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O título deve ter no máximo 150 caracteres.");

    [Fact]
    public void Titulo_com_exatamente_150_caracteres_nao_gera_erro() =>
        _sut.TestValidate(new string('a', 150)).ShouldNotHaveAnyValidationErrors();

    [Theory]
    [InlineData("<script>alert('Pedro')</script>")]
    [InlineData("Visita<img src=x onerror=alert(1)>")]
    [InlineData("Título \"entre aspas\"")]
    [InlineData("Visita `crase`")]
    [InlineData("Visita {{ 7*7 }}")]
    public void Titulo_com_caracteres_nao_permitidos_gera_erro(string titulo) =>
        _sut.TestValidate(titulo).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("O título contém caracteres não permitidos.");
}
