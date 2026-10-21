using euSindico.Api.Validators;
using euSindico.Application.Relatorios.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class GerarRelatorioDtoValidatorTests
{
    private readonly GerarRelatorioDtoValidator _sut = new();

    [Fact]
    public void Mes_e_ano_validos_passam()
    {
        _sut.TestValidate(new GerarRelatorioDto(9, 2026)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData(1)]
    [InlineData(12)]
    public void Limites_do_mes_sao_aceitos(int mes)
    {
        _sut.TestValidate(new GerarRelatorioDto(mes, 2026)).ShouldNotHaveValidationErrorFor(x => x.Mes);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(13)]
    [InlineData(-1)]
    public void Mes_fora_da_faixa_e_rejeitado(int mes)
    {
        _sut.TestValidate(new GerarRelatorioDto(mes, 2026)).ShouldHaveValidationErrorFor(x => x.Mes);
    }

    [Fact]
    public void Mes_ausente_e_rejeitado()
    {
        _sut.TestValidate(new GerarRelatorioDto(null, 2026)).ShouldHaveValidationErrorFor(x => x.Mes);
    }

    [Theory]
    [InlineData(1999)]
    [InlineData(0)]
    [InlineData(10000)]
    public void Ano_fora_da_faixa_e_rejeitado(int ano)
    {
        _sut.TestValidate(new GerarRelatorioDto(9, ano)).ShouldHaveValidationErrorFor(x => x.Ano);
    }

    [Fact]
    public void Ano_ausente_e_rejeitado()
    {
        _sut.TestValidate(new GerarRelatorioDto(9, null)).ShouldHaveValidationErrorFor(x => x.Ano);
    }
}
