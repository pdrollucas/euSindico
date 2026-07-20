using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;

namespace euSindico.Application.Auth;

public class AuthService(
    IUsuarioRepository usuarioRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService)
{
    private static readonly TimeSpan RefreshTokenDuracao = TimeSpan.FromHours(8);

    public async Task<UsuarioDto> RegistrarAsync(RegistrarUsuarioDto dto, CancellationToken ct = default)
    {
        if (await usuarioRepository.ExisteEmailAsync(dto.Email, ct))
        {
            throw new EmailJaCadastradoException(dto.Email);
        }

        var senhaHash = passwordHasher.Hash(dto.Senha);
        var usuario = new Usuario(dto.Nome, dto.Email, senhaHash);

        await usuarioRepository.AdicionarAsync(usuario, ct);

        return new UsuarioDto(usuario.Id, usuario.Nome, usuario.Email, usuario.CriadoEm);
    }

    public async Task<TokenResponseDto> LoginAsync(LoginDto dto, CancellationToken ct = default)
    {
        var usuario = await usuarioRepository.BuscarPorEmailAsync(dto.Email, ct);

        if (usuario is null || !passwordHasher.Verificar(dto.Senha, usuario.SenhaHash))
        {
            throw new CredenciaisInvalidasException();
        }

        return await EmitirTokensAsync(usuario, DateTime.UtcNow.Add(RefreshTokenDuracao), ct);
    }

    public async Task<TokenResponseDto> RenovarTokenAsync(RefreshTokenDto dto, CancellationToken ct = default)
    {
        var hash = tokenService.HashRefreshToken(dto.RefreshToken);
        var refreshTokenAtual = await refreshTokenRepository.BuscarPorHashAsync(hash, ct);

        if (refreshTokenAtual is null || !refreshTokenAtual.EstaAtivo)
        {
            throw new RefreshTokenInvalidoException();
        }

        var usuario = await usuarioRepository.BuscarPorIdAsync(refreshTokenAtual.UsuarioId, ct)
            ?? throw new RefreshTokenInvalidoException();

        // Rotação: este refresh token não serve mais, independente do resultado a seguir.
        refreshTokenAtual.Revogar();
        await refreshTokenRepository.AtualizarAsync(refreshTokenAtual, ct);

        // expiraEm não é estendido — a sessão total continua limitada a 8h desde o login original.
        return await EmitirTokensAsync(usuario, refreshTokenAtual.ExpiraEm, ct);
    }

    private async Task<TokenResponseDto> EmitirTokensAsync(Usuario usuario, DateTime expiraEm, CancellationToken ct)
    {
        var accessToken = tokenService.GerarAccessToken(usuario);
        var refreshTokenGerado = tokenService.GerarRefreshToken();

        var refreshToken = new RefreshToken(usuario.Id, refreshTokenGerado.Hash, expiraEm);
        await refreshTokenRepository.AdicionarAsync(refreshToken, ct);

        return new TokenResponseDto(accessToken, refreshTokenGerado.Token);
    }
}
