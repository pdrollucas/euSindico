using euSindico.Api.Validators;
using euSindico.Application.Equipe.Dtos;
using euSindico.Domain.Enums;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class ConvidarFuncionarioDtoValidatorTests
{
    private readonly ConvidarFuncionarioDtoValidator _sut = new();

    [Theory]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public void Dto_valido_nao_gera_erro(PapelPredio papel)
    {
        var dto = new ConvidarFuncionarioDto("ana@eusindico.com", papel);

        _sut.TestValidate(dto).ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Email_invalido_gera_erro()
    {
        var dto = new ConvidarFuncionarioDto("<script>alert('Pedro')</script>@hot", PapelPredio.Gestor);

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Papel_sindico_gera_erro()
    {
        var dto = new ConvidarFuncionarioDto("ana@eusindico.com", PapelPredio.Sindico);

        _sut.TestValidate(dto).ShouldHaveValidationErrorFor(x => x.Papel);
    }
}
