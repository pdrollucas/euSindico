using System.IdentityModel.Tokens.Jwt;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Security;
using Microsoft.Extensions.Options;

namespace euSindico.Infrastructure.Tests.Security;

public class TokenServiceTests
{
    private readonly TokenService _sut = new(Options.Create(new JwtOptions
    {
        SecretKey = "chave-de-teste-com-pelo-menos-32-bytes-de-tamanho",
        Issuer = "euSindico.Tests",
        Audience = "euSindico.Tests",
        AccessTokenMinutes = 30,
    }));

    [Fact]
    public void GerarAccessToken_produz_jwt_valido_com_claims_sub_e_email()
    {
        var usuario = new Usuario("João Silva", "joao@eusindico.com", "hash-fake");

        var token = _sut.GerarAccessToken(usuario);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal(usuario.Id.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("joao@eusindico.com", jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.True(jwt.ValidTo > DateTime.UtcNow.AddMinutes(29) && jwt.ValidTo <= DateTime.UtcNow.AddMinutes(30));
    }

    [Fact]
    public void GerarRefreshToken_gera_token_diferente_do_hash_e_hash_deterministico()
    {
        var gerado = _sut.GerarRefreshToken();

        Assert.NotEqual(gerado.Token, gerado.Hash);
        Assert.NotEmpty(gerado.Token);
        Assert.NotEmpty(gerado.Hash);
    }

    [Fact]
    public void GerarRefreshToken_chamado_duas_vezes_gera_tokens_diferentes()
    {
        var primeiro = _sut.GerarRefreshToken();
        var segundo = _sut.GerarRefreshToken();

        Assert.NotEqual(primeiro.Token, segundo.Token);
        Assert.NotEqual(primeiro.Hash, segundo.Hash);
    }

    [Fact]
    public void HashRefreshToken_reproduz_o_mesmo_hash_gerado_originalmente_para_o_token()
    {
        var gerado = _sut.GerarRefreshToken();

        var hashRecalculado = _sut.HashRefreshToken(gerado.Token);

        Assert.Equal(gerado.Hash, hashRecalculado);
    }

    [Fact]
    public void HashRefreshToken_para_tokens_diferentes_produz_hashes_diferentes()
    {
        var hash1 = _sut.HashRefreshToken("token-a");
        var hash2 = _sut.HashRefreshToken("token-b");

        Assert.NotEqual(hash1, hash2);
    }

    [Fact]
    public void GerarCodigoRedefinicaoSenha_produz_codigo_de_6_caracteres_so_com_letras_maiusculas_e_numeros_sem_ambiguos()
    {
        var gerado = _sut.GerarCodigoRedefinicaoSenha();

        Assert.Matches("^[ABCDEFGHJKMNPQRSTUVWXYZ23456789]{6}$", gerado.Codigo);
    }

    [Fact]
    public void GerarCodigoRedefinicaoSenha_chamado_duas_vezes_gera_codigos_diferentes()
    {
        var primeiro = _sut.GerarCodigoRedefinicaoSenha();
        var segundo = _sut.GerarCodigoRedefinicaoSenha();

        Assert.NotEqual(primeiro.Codigo, segundo.Codigo);
    }

    [Fact]
    public void GerarCodigoRedefinicaoSenha_hash_corresponde_ao_codigo_gerado()
    {
        var gerado = _sut.GerarCodigoRedefinicaoSenha();

        var hashRecalculado = _sut.HashCodigoRedefinicaoSenha(gerado.Codigo);

        Assert.Equal(gerado.Hash, hashRecalculado);
    }

    [Fact]
    public void HashCodigoRedefinicaoSenha_e_case_insensitive()
    {
        var hashMinusculo = _sut.HashCodigoRedefinicaoSenha("ab12cd");
        var hashMaiusculo = _sut.HashCodigoRedefinicaoSenha("AB12CD");

        Assert.Equal(hashMaiusculo, hashMinusculo);
    }

    [Fact]
    public void HashCodigoRedefinicaoSenha_ignora_espacos_nas_bordas()
    {
        var hashComEspacos = _sut.HashCodigoRedefinicaoSenha("  AB12CD  ");
        var hashSemEspacos = _sut.HashCodigoRedefinicaoSenha("AB12CD");

        Assert.Equal(hashSemEspacos, hashComEspacos);
    }

    [Fact]
    public void HashCodigoRedefinicaoSenha_para_codigos_diferentes_produz_hashes_diferentes()
    {
        var hash1 = _sut.HashCodigoRedefinicaoSenha("AB12CD");
        var hash2 = _sut.HashCodigoRedefinicaoSenha("ZZ99ZZ");

        Assert.NotEqual(hash1, hash2);
    }
}
