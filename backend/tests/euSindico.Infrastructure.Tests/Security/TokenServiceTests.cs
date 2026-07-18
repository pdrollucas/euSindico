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
}
