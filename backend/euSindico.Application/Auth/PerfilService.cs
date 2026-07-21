using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;

namespace euSindico.Application.Auth;

public class PerfilService(
    IUsuarioRepository usuarioRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher)
{
    public async Task<UsuarioDto> ObterPerfilAsync(int usuarioId, CancellationToken ct = default)
    {
        var usuario = await BuscarUsuarioOuFalharAsync(usuarioId, ct);

        return MapearParaDto(usuario);
    }

    public async Task<UsuarioDto> AtualizarPerfilAsync(int usuarioId, AtualizarPerfilDto dto, CancellationToken ct = default)
    {
        var usuario = await BuscarUsuarioOuFalharAsync(usuarioId, ct);

        usuario.AtualizarPerfil(dto.Nome, dto.Email);
        await usuarioRepository.AtualizarAsync(usuario, ct);

        return MapearParaDto(usuario);
    }

    public async Task AlterarSenhaAsync(int usuarioId, AlterarSenhaDto dto, CancellationToken ct = default)
    {
        var usuario = await BuscarUsuarioOuFalharAsync(usuarioId, ct);

        if (!passwordHasher.Verificar(dto.SenhaAtual, usuario.SenhaHash))
        {
            throw new SenhaAtualIncorretaException();
        }

        var novoHash = passwordHasher.Hash(dto.NovaSenha);
        usuario.AlterarSenha(novoHash);
        await usuarioRepository.AtualizarAsync(usuario, ct);

        // Troca de senha derruba todas as sessões ativas (todos os dispositivos) — ver AUTHENTICATION.md.
        await refreshTokenRepository.RevogarTodosDoUsuarioAsync(usuarioId, ct);
    }

    public async Task ExcluirContaAsync(int usuarioId, CancellationToken ct = default)
    {
        await BuscarUsuarioOuFalharAsync(usuarioId, ct);

        await usuarioRepository.ExcluirUsuarioEDadosRelacionadosAsync(usuarioId, ct);
    }

    private async Task<Usuario> BuscarUsuarioOuFalharAsync(int usuarioId, CancellationToken ct) =>
        await usuarioRepository.BuscarPorIdAsync(usuarioId, ct) ?? throw new UsuarioNaoEncontradoException();

    private static UsuarioDto MapearParaDto(Usuario usuario) =>
        new(usuario.Id, usuario.Nome, usuario.Email, usuario.CriadoEm);
}
