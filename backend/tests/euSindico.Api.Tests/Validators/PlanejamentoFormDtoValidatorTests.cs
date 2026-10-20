using euSindico.Api.Validators;
using euSindico.Application.Planejamentos.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class PlanejamentoFormDtoValidatorTests
{
    private readonly PlanejamentoFormDtoValidator _sut = new();

    private static PlanejamentoFormDto DtoValido() =>
        new("Pintura da fachada", null, null, null, null);

    [Fact]
    public void Dto_valido_so_com_titulo_nao_gera_erro_RN11() =>
        _sut.TestValidate(DtoValido()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Dto_valido_com_todos_os_campos_nao_gera_erro()
    {
        var dto = new PlanejamentoFormDto("Pintura", new DateOnly(2030, 1, 1), 15000.50m, "Tintas Ltda.", "Cotar R$ 100");

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("<script>alert(1)</script>")]
    public void Titulo_invalido_gera_erro(string titulo) =>
        _sut.TestValidate(DtoValido() with { Titulo = titulo }).ShouldHaveValidationErrorFor(x => x.Titulo);

    [Fact]
    public void Orcamento_zero_e_valido() =>
        _sut.TestValidate(DtoValido() with { OrcamentoPrevisto = 0m }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Orcamento_no_limite_do_decimal_10_2_e_valido() =>
        _sut.TestValidate(DtoValido() with { OrcamentoPrevisto = 99_999_999.99m }).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Orcamento_negativo_gera_erro() =>
        _sut.TestValidate(DtoValido() with { OrcamentoPrevisto = -0.01m })
            .ShouldHaveValidationErrorFor(x => x.OrcamentoPrevisto)
            .WithErrorMessage("O orçamento previsto não pode ser negativo.");

    [Fact]
    public void Orcamento_acima_do_limite_gera_erro() =>
        _sut.TestValidate(DtoValido() with { OrcamentoPrevisto = 100_000_000m })
            .ShouldHaveValidationErrorFor(x => x.OrcamentoPrevisto)
            .WithErrorMessage("O orçamento previsto excede o valor máximo permitido.");

    [Fact]
    public void Orcamento_com_mais_de_duas_casas_decimais_gera_erro() =>
        _sut.TestValidate(DtoValido() with { OrcamentoPrevisto = 10.123m })
            .ShouldHaveValidationErrorFor(x => x.OrcamentoPrevisto)
            .WithErrorMessage("O orçamento previsto deve ter no máximo 2 casas decimais.");

    [Fact]
    public void Prestador_com_caractere_invalido_gera_erro() =>
        _sut.TestValidate(DtoValido() with { NomePrestadorServico = "<b>Atlas</b>" })
            .ShouldHaveValidationErrorFor(x => x.NomePrestadorServico);

    [Fact]
    public void Detalhes_com_html_gera_erro() =>
        _sut.TestValidate(DtoValido() with { Detalhes = "<img src=x>" })
            .ShouldHaveValidationErrorFor(x => x.Detalhes);

    [Fact]
    public void Detalhes_acima_do_limite_gera_erro() =>
        _sut.TestValidate(DtoValido() with { Detalhes = new string('a', DetalhesValidator.TamanhoMaximo + 1) })
            .ShouldHaveValidationErrorFor(x => x.Detalhes);
}
