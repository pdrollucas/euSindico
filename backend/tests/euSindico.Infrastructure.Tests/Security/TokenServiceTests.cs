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
}
