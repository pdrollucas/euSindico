using euSindico.Api.Validators;
using euSindico.Application.Compromissos.Dtos;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class AlterarConclusaoDtoValidatorTests
{
    private readonly AlterarConclusaoDtoValidator _sut = new();

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Valor_informado_nao_gera_erro(bool concluido) =>
        _sut.TestValidate(new AlterarConclusaoDto(concluido)).ShouldNotHaveAnyValidationErrors();

    [Fact]
    public void Valor_ausente_gera_erro_para_nao_reabrir_por_engano() =>
        _sut.TestValidate(new AlterarConclusaoDto(null))
            .ShouldHaveValidationErrorFor(x => x.Concluido)
            .WithErrorMessage("Informe se o compromisso está concluído.");
}
