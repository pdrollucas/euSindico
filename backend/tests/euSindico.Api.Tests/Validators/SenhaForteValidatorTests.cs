using euSindico.Api.Validators;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class SenhaForteValidatorTests
{
    private readonly SenhaForteValidator _sut = new();

    [Fact]
    public void Senha_valida_nao_gera_erro()
    {
        var resultado = _sut.TestValidate("Senha@123");

        resultado.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Senha_vazia_gera_erro() =>
        _sut.TestValidate(string.Empty).ShouldHaveValidationErrorFor(s => s);

    [Fact]
    public void Senha_com_menos_de_8_caracteres_gera_erro() =>
        _sut.TestValidate("Ab1@567").ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("A senha deve ter no mínimo 8 caracteres.");

    [Fact]
    public void Senha_sem_letra_maiuscula_gera_erro() =>
        _sut.TestValidate("senha@123").ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("A senha deve conter ao menos uma letra maiúscula.");

    [Fact]
    public void Senha_sem_letra_minuscula_gera_erro() =>
        _sut.TestValidate("SENHA@123").ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("A senha deve conter ao menos uma letra minúscula.");

    [Fact]
    public void Senha_sem_numero_gera_erro() =>
        _sut.TestValidate("Senha@abc").ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("A senha deve conter ao menos um número.");

    [Fact]
    public void Senha_sem_caractere_especial_gera_erro() =>
        _sut.TestValidate("Senha1234").ShouldHaveValidationErrorFor(s => s)
            .WithErrorMessage("A senha deve conter ao menos um caractere especial.");
}
