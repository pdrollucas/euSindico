using euSindico.Domain.Entities;
using euSindico.Domain.Enums;

namespace euSindico.Domain.Tests;

public class PredioUsuarioTests
{
    [Fact]
    public void CriarComoDono_define_papel_sindico_e_sem_convidado()
    {
        var vinculo = PredioUsuario.CriarComoDono(predioId: 1, usuarioId: 10);

        Assert.Equal(1, vinculo.PredioId);
        Assert.Equal(10, vinculo.UsuarioId);
        Assert.Equal(PapelPredio.Sindico, vinculo.Papel);
        Assert.Null(vinculo.ConvidadoPorUsuarioId);
    }

    [Theory]
    [InlineData(PapelPredio.Gestor)]
    [InlineData(PapelPredio.Colaborador)]
    public void CriarComoConvidado_com_papel_valido_define_campos_corretamente(PapelPredio papel)
    {
        var vinculo = PredioUsuario.CriarComoConvidado(predioId: 1, usuarioId: 20, papel, convidadoPorUsuarioId: 10);

        Assert.Equal(1, vinculo.PredioId);
        Assert.Equal(20, vinculo.UsuarioId);
        Assert.Equal(papel, vinculo.Papel);
        Assert.Equal(10, vinculo.ConvidadoPorUsuarioId);
    }

    [Fact]
    public void CriarComoConvidado_com_papel_sindico_lanca_excecao()
    {
        Assert.Throws<ArgumentException>(
            () => PredioUsuario.CriarComoConvidado(predioId: 1, usuarioId: 20, PapelPredio.Sindico, convidadoPorUsuarioId: 10));
    }
}
