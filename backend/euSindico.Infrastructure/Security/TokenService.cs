using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace euSindico.Infrastructure.Security;

public class TokenService(IOptions<JwtOptions> jwtOptions) : ITokenService
{
    private readonly JwtOptions _options = jwtOptions.Value;

    public string GerarAccessToken(Usuario usuario)
    {
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, usuario.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, usuario.Email),
        };

        var chave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SecretKey));
        var credenciais = new SigningCredentials(chave, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_options.AccessTokenMinutes),
            signingCredentials: credenciais);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public RefreshTokenGerado GerarRefreshToken()
    {
        var bytesAleatorios = RandomNumberGenerator.GetBytes(32);
        var token = Convert.ToBase64String(bytesAleatorios);
        var hash = CalcularHash(token);

        return new RefreshTokenGerado(token, hash);
    }

    public string HashRefreshToken(string refreshToken) => CalcularHash(refreshToken);

    private static string CalcularHash(string refreshToken)
    {
        var bytesHash = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytesHash);
    }
}
