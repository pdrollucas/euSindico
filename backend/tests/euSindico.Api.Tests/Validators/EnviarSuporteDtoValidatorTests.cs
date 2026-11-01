using euSindico.Api.Validators;
using euSindico.Application.Suporte;
using FluentValidation.TestHelper;

namespace euSindico.Api.Tests.Validators;

public class EnviarSuporteDtoValidatorTests
{
    private const string MensagemValida = "Mensagem com tamanho suficiente.";

    private readonly EnviarSuporteDtoValidator _sut = new();

    [Fact]
    public void Dto_valido_nao_gera_erro()
    {
        _sut.TestValidate(new EnviarSuporteDto("Erro no relatório", MensagemValida)).ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    public void Assunto_vazio_ou_curto_gera_erro(string assunto)
    {
        _sut.TestValidate(new EnviarSuporteDto(assunto, MensagemValida)).ShouldHaveValidationErrorFor(x => x.Assunto);
    }

    [Fact]
    public void Assunto_acima_de_100_caracteres_gera_erro()
    {
        _sut.TestValidate(new EnviarSuporteDto(new string('a', 101), MensagemValida)).ShouldHaveValidationErrorFor(x => x.Assunto);
    }

    [Fact]
    public void Assunto_com_quebra_de_linha_gera_erro()
    {
        _sut.TestValidate(new EnviarSuporteDto("Assunto\nBcc: x@y.com", MensagemValida)).ShouldHaveValidationErrorFor(x => x.Assunto);
    }

    [Theory]
    [InlineData("")]
    [InlineData("curta")]
    public void Mensagem_vazia_ou_curta_gera_erro(string mensagem)
    {
        _sut.TestValidate(new EnviarSuporteDto("Assunto válido", mensagem)).ShouldHaveValidationErrorFor(x => x.Mensagem);
    }

    [Fact]
    public void Mensagem_acima_de_2000_caracteres_gera_erro()
    {
        _sut.TestValidate(new EnviarSuporteDto("Assunto válido", new string('a', 2001))).ShouldHaveValidationErrorFor(x => x.Mensagem);
    }
}
