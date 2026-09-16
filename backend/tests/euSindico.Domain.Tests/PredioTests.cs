using euSindico.Domain.Entities;

namespace euSindico.Domain.Tests;

public class PredioTests
{
    [Fact]
    public void Construtor_define_dados_iniciais_e_nao_excluido()
    {
        var antes = DateTime.UtcNow;
        var predio = new Predio("Edifício Aurora", "Rua X, 100", usuarioId: 1);

        Assert.Equal("Edifício Aurora", predio.Nome);
        Assert.Equal("Rua X, 100", predio.Endereco);
        Assert.Equal(1, predio.UsuarioId);
        Assert.False(predio.Excluido);
        Assert.Null(predio.ExcluidoEm);
        Assert.InRange(predio.CriadoEm, antes, DateTime.UtcNow);
    }
}
