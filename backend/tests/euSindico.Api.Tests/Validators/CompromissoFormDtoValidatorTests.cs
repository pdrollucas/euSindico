using euSindico.Api.Validators;
using euSindico.Application.Compromissos.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class CompromissoFormDtoValidatorTests
{
    private readonly CompromissoFormDtoValidator _sut = new();

    private static CompromissoFormDto DtoValido() =>
        new(10, "Visita técnica", new DateOnly(2026, 10, 17), new TimeOnly(14, 30), null, null, null);

    [Fact]
    public void Dto_valido_so_com_campos_obrigatorios_nao_gera_erro() =>
        _sut.TestValidate(DtoValido()).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Dto_valido_com_todos_os_campos_nao_gera_erro()
    {
        var dto = DtoValido() with { NomePrestadorServico = "Atlas Ltda.", Detalhes = "Revisar R$ 100", ResponsavelUsuarioId = 5 };

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Data_no_passado_e_permitida() =>
        _sut.TestValidate(DtoValido() with { DataCompromisso = new DateOnly(2020, 1, 1) })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Horario_meia_noite_e_valido() =>
        _sut.TestValidate(DtoValido() with { HorarioCompromisso = new TimeOnly(0, 0) })
            .ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Data_ausente_gera_erro_RN10() =>
        _sut.TestValidate(DtoValido() with { DataCompromisso = null })
            .ShouldHaveValidationErrorFor(x => x.DataCompromisso)
            .WithErrorMessage("A data do compromisso é obrigatória.");

    [Fact]
    public void Horario_ausente_gera_erro_RN10() =>
        _sut.TestValidate(DtoValido() with { HorarioCompromisso = null })
            .ShouldHaveValidationErrorFor(x => x.HorarioCompromisso)
            .WithErrorMessage("O horário do compromisso é obrigatório.");

    [Theory]
    [InlineData(null)]
    [InlineData(0)]
    [InlineData(-1)]
    public void Predio_nao_informado_gera_erro_RN03(int? predioId) =>
        _sut.TestValidate(DtoValido() with { PredioId = predioId })
            .ShouldHaveValidationErrorFor(x => x.PredioId)
            .WithErrorMessage("O prédio é obrigatório.");

    [Fact]
    public void Titulo_invalido_gera_erro() =>
        _sut.TestValidate(DtoValido() with { Titulo = "<script>alert(1)</script>" })
            .ShouldHaveValidationErrorFor(x => x.Titulo);

    [Fact]
    public void Prestador_invalido_gera_erro() =>
        _sut.TestValidate(DtoValido() with { NomePrestadorServico = "<b>x</b>" })
            .ShouldHaveValidationErrorFor(x => x.NomePrestadorServico);

    [Fact]
    public void Detalhes_invalidos_geram_erro() =>
        _sut.TestValidate(DtoValido() with { Detalhes = new string('a', 2001) })
            .ShouldHaveValidationErrorFor(x => x.Detalhes);

    [Theory]
    [InlineData("0")]
    [InlineData("1500.50")]
    [InlineData("99999999.99")]
    public void Orcamento_valido_nao_gera_erro(string valor) =>
        _sut.TestValidate(DtoValido() with { Orcamento = decimal.Parse(valor, System.Globalization.CultureInfo.InvariantCulture) })
            .ShouldNotHaveValidationErrorFor(x => x.Orcamento);

    [Fact]
    public void Orcamento_negativo_gera_erro() =>
        _sut.TestValidate(DtoValido() with { Orcamento = -0.01m })
            .ShouldHaveValidationErrorFor(x => x.Orcamento)
            .WithErrorMessage("O orçamento não pode ser negativo.");

    [Fact]
    public void Orcamento_acima_do_maximo_gera_erro() =>
        _sut.TestValidate(DtoValido() with { Orcamento = 100_000_000m })
            .ShouldHaveValidationErrorFor(x => x.Orcamento)
            .WithErrorMessage("O orçamento excede o valor máximo permitido.");

    [Fact]
    public void Orcamento_com_mais_de_duas_casas_gera_erro() =>
        _sut.TestValidate(DtoValido() with { Orcamento = 10.123m })
            .ShouldHaveValidationErrorFor(x => x.Orcamento)
            .WithErrorMessage("O orçamento deve ter no máximo 2 casas decimais.");

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Responsavel_informado_mas_invalido_gera_erro(int responsavelId) =>
        _sut.TestValidate(DtoValido() with { ResponsavelUsuarioId = responsavelId })
            .ShouldHaveValidationErrorFor(x => x.ResponsavelUsuarioId);
}
