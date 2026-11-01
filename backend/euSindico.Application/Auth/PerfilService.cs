using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Documentos;
using euSindico.Application.Relatorios;
using euSindico.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace euSindico.Application.Auth;

public class PerfilService(
    IUsuarioRepository usuarioRepository,
    IRefreshTokenRepository refreshTokenRepository,
    ICodigoTrocaEmailRepository codigoTrocaEmailRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IEmailSender emailSender,
    IFileStorageService fileStorage,
    ILogger<PerfilService> logger)
{
    private static readonly TimeSpan CodigoTrocaEmailDuracao = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan CooldownSolicitacaoCodigo = TimeSpan.FromMinutes(2);

    public async Task<UsuarioDto> ObterPerfilAsync(int usuarioId, CancellationToken ct = default)
    {
        var usuario = await BuscarUsuarioOuFalharAsync(usuarioId, ct);

        return MapearParaDto(usuario);
    }

    public async Task<UsuarioDto> AtualizarPerfilAsync(int usuarioId, AtualizarPerfilDto dto, CancellationToken ct = default)
    {
        var usuario = await BuscarUsuarioOuFalharAsync(usuarioId, ct);

        // O e-mail não passa por aqui: a troca exige senha e código enviado ao novo endereço
        // (SolicitarTrocaEmailAsync / ConfirmarTrocaEmailAsync).
        usuario.AtualizarNome(dto.Nome);
        await usuarioRepository.AtualizarAsync(usuario, ct);

        return MapearParaDto(usuario);
    }

    public async Task SolicitarTrocaEmailAsync(int usuarioId, SolicitarTrocaEmailDto dto, CancellationToken ct = default)
    {
        var usuario = await BuscarUsuarioOuFalharAsync(usuarioId, ct);

        if (!passwordHasher.Verificar(dto.SenhaAtual, usuario.SenhaHash))
        {
            throw new SenhaAtualIncorretaException();
        }

        // Cobre também o "trocar para o próprio e-mail", já que o atual também está cadastrado.
        if (await usuarioRepository.ExisteEmailAsync(dto.NovoEmail, ct))
        {
            throw new EmailJaCadastradoException(dto.NovoEmail);
        }

        // Usuário autenticado e com senha confirmada: pode receber feedback explícito do cooldown
        // (no reset de senha, anônimo, o cooldown é silencioso por causa da anti-enumeração).
        var ultimoCodigo = await codigoTrocaEmailRepository.BuscarUltimoDoUsuarioAsync(usuarioId, ct);
        if (ultimoCodigo is not null && ultimoCodigo.CriadoEm > DateTime.UtcNow.Subtract(CooldownSolicitacaoCodigo))
        {
            throw new SolicitacaoCodigoRecenteException();
        }

        await codigoTrocaEmailRepository.InvalidarTodosDoUsuarioAsync(usuarioId, ct);

        // Mesmo formato do código de redefinição de senha (6 caracteres, sem ambíguos).
        var codigoGerado = tokenService.GerarCodigoRedefinicaoSenha();
        var codigo = new CodigoTrocaEmail(usuarioId, dto.NovoEmail, codigoGerado.Hash, DateTime.UtcNow.Add(CodigoTrocaEmailDuracao));
        await codigoTrocaEmailRepository.AdicionarAsync(codigo, ct);

        // O código vai para o NOVO endereço: é a prova de que ele existe e é do usuário.
        await emailSender.EnviarAsync(
            dto.NovoEmail,
            "Confirmação de novo e-mail - euSíndico",
            $"Seu código para confirmar este e-mail no euSíndico é: {codigoGerado.Codigo}\n\n" +
            $"Ele expira em {(int)CodigoTrocaEmailDuracao.TotalMinutes} minutos. Se você não solicitou isso, ignore este e-mail.",
            ct);
    }

    public async Task<UsuarioDto> ConfirmarTrocaEmailAsync(int usuarioId, ConfirmarTrocaEmailDto dto, CancellationToken ct = default)
    {
        var usuario = await BuscarUsuarioOuFalharAsync(usuarioId, ct);

        var hash = tokenService.HashCodigoRedefinicaoSenha(dto.Codigo);
        var codigo = await codigoTrocaEmailRepository.BuscarPorUsuarioIdEHashAsync(usuarioId, hash, ct);

        if (codigo is null || !codigo.EstaValido)
        {
            throw new CodigoRedefinicaoInvalidoException();
        }

        // Reconfere: outra conta pode ter ocupado o e-mail entre a solicitação e a confirmação.
        if (await usuarioRepository.ExisteEmailAsync(codigo.NovoEmail, ct))
        {
            throw new EmailJaCadastradoException(codigo.NovoEmail);
        }

        var emailAntigo = usuario.Email;

        codigo.MarcarComoUsado();
        await codigoTrocaEmailRepository.AtualizarAsync(codigo, ct);

        usuario.AlterarEmail(codigo.NovoEmail);
        await usuarioRepository.AtualizarAsync(usuario, ct);

        await NotificarAsync(
            emailAntigo,
            "Seu e-mail foi alterado - euSíndico",
            "O e-mail da sua conta no euSíndico foi alterado. A partir de agora, o acesso é feito com o novo endereço.\n\n" +
            "Se não foi você, redefina sua senha imediatamente e entre em contato com o suporte.",
            ct);

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

        await NotificarAsync(
            usuario.Email,
            "Sua senha foi alterada - euSíndico",
            "A senha da sua conta no euSíndico foi alterada e todas as sessões foram encerradas.\n\n" +
            "Se não foi você, redefina sua senha imediatamente e entre em contato com o suporte.",
            ct);
    }

    public async Task ExcluirContaAsync(int usuarioId, ExcluirContaDto dto, CancellationToken ct = default)
    {
        var usuario = await BuscarUsuarioOuFalharAsync(usuarioId, ct);

        // Ação irreversível: a sessão aberta sozinha não basta, exige a senha de novo.
        if (!passwordHasher.Verificar(dto.Senha, usuario.SenhaHash))
        {
            throw new SenhaAtualIncorretaException();
        }

        var emailDoUsuario = usuario.Email;

        var prediosApagados = await usuarioRepository.ExcluirUsuarioEDadosRelacionadosAsync(usuarioId, ct);

        // RFC 6.2: a exclusão é permanente, o que inclui os arquivos no armazenamento. Só depois de a
        // transação do banco confirmar — se ela falhar, nada é apagado do S3. Só Síndico tem prédios
        // apagados; conta de funcionário devolve lista vazia e não toca no armazenamento.
        foreach (var predioId in prediosApagados)
        {
            await LimparArquivosDoPredioAsync(predioId);
        }

        await NotificarAsync(
            emailDoUsuario,
            "Sua conta foi excluída - euSíndico",
            "Sua conta no euSíndico e todos os dados vinculados a ela foram excluídos permanentemente.\n\n" +
            "Se não foi você, entre em contato com o suporte.",
            ct);
    }

    // Aviso de segurança depois que a operação já foi concluída: falha no envio não pode desfazer nem
    // virar erro para o usuário (a troca/exclusão já aconteceu), então só vai para o log.
    private async Task NotificarAsync(string destinatario, string assunto, string corpoTexto, CancellationToken ct)
    {
        try
        {
            await emailSender.EnviarAsync(destinatario, assunto, corpoTexto, ct);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Falha ao enviar aviso de segurança por e-mail: {Assunto}", assunto);
        }
    }

    // A conta já foi excluída: falha no armazenamento não pode virar erro para o usuário (ele
    // repetiria um pedido que não tem mais o que apagar). Fica como arquivo órfão e o prefixo vai
    // para o log, para limpeza manual.
    private async Task LimparArquivosDoPredioAsync(int predioId)
    {
        // Cada prefixo é tentado de forma independente: falhar nos relatórios não impede limpar os documentos.
        foreach (var prefixo in new[] { ChaveRelatorio.Prefixo(predioId), ChaveDocumento.Prefixo(predioId) })
        {
            try
            {
                await fileStorage.RemoverPrefixoAsync(prefixo, CancellationToken.None);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Arquivos órfãos no armazenamento após exclusão de conta: {Prefixo}", prefixo);
            }
        }
    }

    private async Task<Usuario> BuscarUsuarioOuFalharAsync(int usuarioId, CancellationToken ct) =>
        await usuarioRepository.BuscarPorIdAsync(usuarioId, ct) ?? throw new UsuarioNaoEncontradoException();

    private static UsuarioDto MapearParaDto(Usuario usuario) =>
        new(usuario.Id, usuario.Nome, usuario.Email, usuario.CriadoEm);
}
