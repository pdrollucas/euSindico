using euSindico.Infrastructure.Security;

namespace euSindico.Infrastructure.Tests.Security;

public class PasswordHasherTests
{
    private readonly PasswordHasher _sut = new();

    [Fact]
    public void Hash_gera_valor_diferente_da_senha_original_e_verificavel_pelo_BCrypt()
    {
        var hash = _sut.Hash("Senha@123");

        Assert.NotEqual("Senha@123", hash);
        Assert.True(BCrypt.Net.BCrypt.Verify("Senha@123", hash));
    }

    [Fact]
    public void Hash_da_mesma_senha_duas_vezes_gera_hashes_diferentes_por_causa_do_salt()
    {
        var hash1 = _sut.Hash("Senha@123");
        var hash2 = _sut.Hash("Senha@123");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void Verificar_retorna_true_para_a_senha_correta_e_false_para_a_errada()
    {
        var hash = _sut.Hash("Senha@123");

        Assert.True(_sut.Verificar("Senha@123", hash));
        Assert.False(_sut.Verificar("SenhaErrada@1", hash));
    }
}
