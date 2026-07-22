using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;

namespace euSindico.Application.Auth;

public class AuthService(
    IUsuarioRepository usuarioRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ICodigoRedefinicaoSenhaRepository codigoRedefinicaoSenhaRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IEmailSender emailSender)
{
    private static readonly TimeSpan RefreshTokenDuracao = TimeSpan.FromHours(8);
    private static readonly TimeSpan CodigoRedefinicaoDuracao = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan CooldownSolicitacaoCodigo = TimeSpan.FromMinutes(5);

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

    public async Task LogoutAsync(int usuarioId, RefreshTokenDto dto, CancellationToken ct = default)
    {
        var hash = tokenService.HashRefreshToken(dto.RefreshToken);
        var refreshToken = await refreshTokenRepository.BuscarPorHashAsync(hash, ct);

        // Idempotente: se o token não existe, já foi revogado/expirou, ou pertence a
        // outro usuário, o logout "funciona" do mesmo jeito — o estado desejado (essa
        // sessão não renova mais) já vale, e não damos nenhum sinal ao cliente sobre
        // qual desses casos aconteceu (mesmo espírito anti-enumeração do RenovarTokenAsync).
        if (refreshToken is not null && refreshToken.UsuarioId == usuarioId && refreshToken.EstaAtivo)
        {
            refreshToken.Revogar();
            await refreshTokenRepository.AtualizarAsync(refreshToken, ct);
        }
    }

    public async Task SolicitarRedefinicaoSenhaAsync(EsqueciSenhaDto dto, CancellationToken ct = default)
    {
        var usuario = await usuarioRepository.BuscarPorEmailAsync(dto.Email, ct);

        // Resposta idêntica exista ou não o e-mail, e também se o cooldown estiver ativo —
        // só age quando o e-mail existe e passaram 5+ minutos desde o último código
        // (RN15, anti-enumeração e anti-spam de e-mail, ver SECURITY.md seção 10).
        if (usuario is null)
        {
            return;
        }

        var ultimoCodigo = await codigoRedefinicaoSenhaRepository.BuscarUltimoDoUsuarioAsync(usuario.Id, ct);
        if (ultimoCodigo is not null && ultimoCodigo.CriadoEm > DateTime.UtcNow.Subtract(CooldownSolicitacaoCodigo))
        {
            return;
        }

        await codigoRedefinicaoSenhaRepository.InvalidarTodosDoUsuarioAsync(usuario.Id, ct);

        var codigoGerado = tokenService.GerarCodigoRedefinicaoSenha();
        var codigo = new CodigoRedefinicaoSenha(usuario.Id, codigoGerado.Hash, DateTime.UtcNow.Add(CodigoRedefinicaoDuracao));
        await codigoRedefinicaoSenhaRepository.AdicionarAsync(codigo, ct);

        await emailSender.EnviarAsync(
            usuario.Email,
            "Redefinição de senha - euSíndico",
            $"Seu código de redefinição de senha é: {codigoGerado.Codigo}\n\n" +
            $"Ele expira em {(int)CodigoRedefinicaoDuracao.TotalMinutes} minutos. Se você não solicitou isso, ignore este e-mail.",
            ct);
    }

    public async Task VerificarCodigoRedefinicaoAsync(VerificarCodigoDto dto, CancellationToken ct = default)
    {
        await ObterCodigoValidoOuFalharAsync(dto.Email, dto.Codigo, ct);
    }

    public async Task RedefinirSenhaAsync(RedefinirSenhaDto dto, CancellationToken ct = default)
    {
        var (usuario, codigo) = await ObterCodigoValidoOuFalharAsync(dto.Email, dto.Codigo, ct);

        codigo.MarcarComoUsado();
        await codigoRedefinicaoSenhaRepository.AtualizarAsync(codigo, ct);

        var novoHash = passwordHasher.Hash(dto.NovaSenha);
        usuario.AlterarSenha(novoHash);
        await usuarioRepository.AtualizarAsync(usuario, ct);

        // Mesma regra da troca de senha autenticada: derruba todas as sessões ativas.
        await refreshTokenRepository.RevogarTodosDoUsuarioAsync(usuario.Id, ct);
    }

    // Reaplicado tanto na verificação (passo de UX) quanto na redefinição de fato (passo de
    // segurança, que nunca confia só na verificação anterior) — mesma mensagem genérica pra
    // "e-mail não existe", "código errado", "expirado" ou "já usado" (anti-enumeração).
    private async Task<(Usuario Usuario, CodigoRedefinicaoSenha Codigo)> ObterCodigoValidoOuFalharAsync(
        string email, string codigoDigitado, CancellationToken ct)
    {
        var usuario = await usuarioRepository.BuscarPorEmailAsync(email, ct);
        var hash = tokenService.HashCodigoRedefinicaoSenha(codigoDigitado);

        var codigo = usuario is null
            ? null
            : await codigoRedefinicaoSenhaRepository.BuscarPorUsuarioIdEHashAsync(usuario.Id, hash, ct);

        if (usuario is null || codigo is null || !codigo.EstaValido)
        {
            throw new CodigoRedefinicaoInvalidoException();
        }

        return (usuario, codigo);
    }

    private async Task<TokenResponseDto> EmitirTokensAsync(Usuario usuario, DateTime expiraEm, CancellationToken ct)
    {
        var accessToken = tokenService.GerarAccessToken(usuario);
        var refreshTokenGerado = tokenService.GerarRefreshToken();

        var refreshToken = new RefreshToken(usuario.Id, refreshTokenGerado.Hash, expiraEm);
        await refreshTokenRepository.AdicionarAsync(refreshToken, ct);

        return new TokenResponseDto(accessToken, refreshTokenGerado.Token, expiraEm);
    }
}
