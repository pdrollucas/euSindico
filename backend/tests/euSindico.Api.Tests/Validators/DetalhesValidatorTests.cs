using euSindico.Api.Validators;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class DetalhesValidatorTests
{
    private readonly DetalhesValidator _sut = new();

    [Theory]
    [InlineData("Orçamento de R$ 1.200,00 com 50% de entrada")]
    [InlineData("Contato: maria@exemplo.com.br")]
    [InlineData("Levar a chave \"B\" do bloco\nConferir o relatório #3")]
    public void Texto_livre_legitimo_nao_gera_erro(string detalhes) =>
        _sut.TestValidate(detalhes).ShouldNotHaveAnyValidationErrors();

    // Null não passa por aqui: o FluentValidation não executa o validador filho de uma propriedade nula
    // (coberto em CompromissoFormDtoValidatorTests); vazio/espaços chegam e também são aceitos.
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Detalhes_ausentes_sao_permitidos_por_serem_opcionais(string detalhes) =>
        _sut.TestValidate(detalhes).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Detalhes_com_exatamente_2000_caracteres_nao_geram_erro() =>
        _sut.TestValidate(new string('a', 2000)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Detalhes_maiores_que_2000_caracteres_geram_erro() =>
        _sut.TestValidate(new string('a', 2001)).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("Os detalhes devem ter no máximo 2000 caracteres.");

    [Theory]
    [InlineData("<script>alert(1)</script>")]
    [InlineData("texto <b>negrito</b>")]
    [InlineData("a > b")]
    public void Detalhes_com_sinais_de_menor_ou_maior_geram_erro(string detalhes) =>
        _sut.TestValidate(detalhes).ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("Os detalhes contêm caracteres não permitidos.");
}
