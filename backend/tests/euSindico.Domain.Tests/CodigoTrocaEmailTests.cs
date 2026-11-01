using euSindico.Domain.Entities;

namespace euSindico.Domain.Tests;

public class CodigoTrocaEmailTests
{
    [Fact]
    public void Constructor_define_campos_corretamente()
    {
        var expiraEm = DateTime.UtcNow.AddMinutes(15);

        var codigo = new CodigoTrocaEmail(usuarioId: 1, "novo@eusindico.com", "hash-fake", expiraEm);

        Assert.Equal(1, codigo.UsuarioId);
        Assert.Equal("novo@eusindico.com", codigo.NovoEmail);
        Assert.Equal("hash-fake", codigo.CodigoHash);
        Assert.Equal(expiraEm, codigo.ExpiraEm);
        Assert.Null(codigo.UsadoEm);
        Assert.True(codigo.EstaValido);
    }

    [Fact]
    public void EstaValido_e_falso_depois_de_expirar()
    {
        var codigo = new CodigoTrocaEmail(1, "novo@eusindico.com", "hash-fake", DateTime.UtcNow.AddSeconds(-1));

        Assert.False(codigo.EstaValido);
    }

    [Fact]
    public void MarcarComoUsado_invalida_o_codigo()
    {
        var codigo = new CodigoTrocaEmail(1, "novo@eusindico.com", "hash-fake", DateTime.UtcNow.AddMinutes(15));

        codigo.MarcarComoUsado();

        Assert.NotNull(codigo.UsadoEm);
        Assert.False(codigo.EstaValido);
    }
}
