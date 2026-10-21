using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Relatorios;
using euSindico.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace euSindico.Application.Auth;

public class PerfilService(
    IUsuarioRepository usuarioRepository,
    IRefreshTokenRepository refreshTokenRepository,
    IPasswordHasher passwordHasher,
    IFileStorageService fileStorage,
    ILogger<PerfilService> logger)
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

        var prediosApagados = await usuarioRepository.ExcluirUsuarioEDadosRelacionadosAsync(usuarioId, ct);

        // RFC 6.2: a exclusão é permanente, o que inclui os arquivos no armazenamento. Só depois de a
        // transação do banco confirmar — se ela falhar, nada é apagado do S3. Só Síndico tem prédios
        // apagados; conta de funcionário devolve lista vazia e não toca no armazenamento.
        foreach (var predioId in prediosApagados)
        {
            await LimparArquivosDoPredioAsync(predioId);
        }
    }

    // A conta já foi excluída: falha no armazenamento não pode virar erro para o usuário (ele
    // repetiria um pedido que não tem mais o que apagar). Fica como arquivo órfão e o prefixo vai
    // para o log, para limpeza manual.
    private async Task LimparArquivosDoPredioAsync(int predioId)
    {
        try
        {
            await fileStorage.RemoverPrefixoAsync(ChaveRelatorio.Prefixo(predioId), CancellationToken.None);
        }
        catch (Exception ex)
        {
            logger.LogWarning(
                ex,
                "Arquivos órfãos no armazenamento após exclusão de conta: {Prefixo}",
                ChaveRelatorio.Prefixo(predioId));
        }
    }

    private async Task<Usuario> BuscarUsuarioOuFalharAsync(int usuarioId, CancellationToken ct) =>
        await usuarioRepository.BuscarPorIdAsync(usuarioId, ct) ?? throw new UsuarioNaoEncontradoException();

    private static UsuarioDto MapearParaDto(Usuario usuario) =>
        new(usuario.Id, usuario.Nome, usuario.Email, usuario.CriadoEm);
}
