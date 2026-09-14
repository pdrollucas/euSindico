using System.Net;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe.Dtos;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;

namespace euSindico.Application.Equipe;

public class EquipeService(
    IPredioUsuarioRepository predioUsuarioRepository,
    IConviteFuncionarioRepository conviteFuncionarioRepository,
    IUsuarioRepository usuarioRepository,
    IPasswordHasher passwordHasher,
    ITokenService tokenService,
    IEmailSender emailSender,
    IConviteLinkBuilder conviteLinkBuilder,
    AutorizacaoPredioService autorizacaoPredioService)
{
    // Proposta registrada em EQUIPE.md ("Decisões desta etapa") — prazo generoso o bastante
    // para o convidado ver o e-mail com calma, sem deixar convites pendentes indefinidamente.
    private static readonly TimeSpan ValidadeConvite = TimeSpan.FromDays(7);

    public async Task ConvidarAsync(int usuarioId, int predioId, ConvidarFuncionarioDto dto, CancellationToken ct = default)
    {
        // O vínculo do próprio síndico já vem com Usuario/Predio carregados (mesma consulta
        // da autorização) — reaproveitado abaixo pro nome do síndico e do prédio no e-mail,
        // sem uma segunda consulta.
        var vinculoSindico = await autorizacaoPredioService.VerificarAcessoAsync(usuarioId, predioId, AcaoPredio.GerenciarEquipe, ct);

        if (await predioUsuarioRepository.ExisteMembroComEmailAsync(predioId, dto.Email, ct) ||
            await conviteFuncionarioRepository.ExisteConvitePendenteAsync(predioId, dto.Email, ct))
        {
            throw new EmailJaVinculadoException(dto.Email);
        }

        var tokenGerado = tokenService.GerarTokenConvite();
        var convite = new ConviteFuncionario(predioId, dto.Email, dto.Papel, tokenGerado.Hash, usuarioId, DateTime.UtcNow.Add(ValidadeConvite));
        await conviteFuncionarioRepository.AdicionarAsync(convite, ct);

        var link = conviteLinkBuilder.Construir(tokenGerado.Token);
        var sindicoNome = vinculoSindico.Usuario!.Nome;
        var predioNome = vinculoSindico.Predio!.Nome;
        var papelDescricao = DescricaoPapel(dto.Papel);
        var diasValidade = (int)ValidadeConvite.TotalDays;

        var corpoTexto =
            $"{sindicoNome} convidou você para fazer parte da equipe do prédio \"{predioNome}\" no euSíndico, como {papelDescricao}.\n\n" +
            $"Acesse o link para aceitar o convite: {link}\n\n" +
            $"Este link expira em {diasValidade} dias. Se você não esperava este convite, ignore este e-mail.";

        // HTML-encoding de nome/prédio por precaução (defesa em profundidade, mesmo raciocínio
        // do SECURITY.md seção 3 — "qualquer consumidor futuro desse dado, como e-mail de
        // notificação, precisa lembrar de escapar corretamente"): o Predio ainda não tem
        // validação de formato própria (PREDIOS.md ainda não implementado), então o nome
        // não tem garantia hoje de estar livre de caracteres HTML.
        var sindicoNomeSeguro = WebUtility.HtmlEncode(sindicoNome);
        var predioNomeSeguro = WebUtility.HtmlEncode(predioNome);
        var linkSeguro = WebUtility.HtmlEncode(link);

        var corpoHtml =
            $"""
            <div style="font-family: Arial, sans-serif; color: #1B2536; max-width: 480px; margin: 0 auto;">
              <p><strong>{sindicoNomeSeguro}</strong> convidou você para fazer parte da equipe do prédio <strong>{predioNomeSeguro}</strong> no euSíndico, como <strong>{papelDescricao}</strong>.</p>
              <p style="text-align: center; margin: 32px 0;">
                <a href="{linkSeguro}" style="background-color: #1E4FA8; color: #FFFFFF; text-decoration: none; padding: 12px 32px; border-radius: 8px; font-weight: bold; display: inline-block;">Aceitar convite</a>
              </p>
              <p style="font-size: 13px; color: #5B6B85;">Este link expira em {diasValidade} dias. Se você não esperava este convite, ignore este e-mail.</p>
              <p style="font-size: 12px; color: #9AA6B8;">Se o botão não funcionar, copie e cole este link no navegador:<br>{linkSeguro}</p>
            </div>
            """;

        await emailSender.EnviarAsync(dto.Email, "Convite para a equipe - euSíndico", corpoTexto, ct, corpoHtml);
    }

    public async Task<List<EquipeMembroDto>> ListarEquipeAsync(int usuarioId, int predioId, CancellationToken ct = default)
    {
        await autorizacaoPredioService.VerificarAcessoAsync(usuarioId, predioId, AcaoPredio.GerenciarEquipe, ct);

        var membros = await predioUsuarioRepository.ListarDoPredioAsync(predioId, ct);
        return membros
            .Select(m => new EquipeMembroDto(m.UsuarioId, m.Usuario!.Nome, m.Usuario.Email, m.Papel, m.CriadoEm))
            .ToList();
    }

    public async Task RemoverAsync(int usuarioId, int predioId, int usuarioAlvoId, CancellationToken ct = default)
    {
        await autorizacaoPredioService.VerificarAcessoAsync(usuarioId, predioId, AcaoPredio.GerenciarEquipe, ct);

        if (usuarioAlvoId == usuarioId)
        {
            throw new RemoverDonoDoPredioException();
        }

        var vinculo = await predioUsuarioRepository.BuscarVinculoAsync(predioId, usuarioAlvoId, ct)
            ?? throw new MembroNaoEncontradoException();

        await predioUsuarioRepository.RemoverAsync(vinculo, ct);
    }

    public async Task<ConviteDetalheDto> ObterDetalheConviteAsync(string token, CancellationToken ct = default)
    {
        var convite = await ObterConviteValidoOuFalharAsync(token, ct);
        var contaJaExiste = await usuarioRepository.ExisteEmailAsync(convite.Email, ct);

        return new ConviteDetalheDto(convite.Predio!.Nome, convite.Papel, contaJaExiste, convite.ExpiraEm);
    }

    public async Task AceitarConviteAsync(string token, AceitarConviteDto dto, CancellationToken ct = default)
    {
        var convite = await ObterConviteValidoOuFalharAsync(token, ct);
        var usuario = await usuarioRepository.BuscarPorEmailAsync(convite.Email, ct);

        if (usuario is null)
        {
            if (string.IsNullOrWhiteSpace(dto.Nome) || string.IsNullOrWhiteSpace(dto.Senha))
            {
                throw new DadosCadastroObrigatoriosException();
            }

            var senhaHash = passwordHasher.Hash(dto.Senha);
            usuario = new Usuario(dto.Nome, convite.Email, senhaHash);
            await usuarioRepository.AdicionarAsync(usuario, ct);
        }

        var vinculo = PredioUsuario.CriarComoConvidado(convite.PredioId, usuario.Id, convite.Papel, convite.CriadoPorUsuarioId);
        await predioUsuarioRepository.AdicionarAsync(vinculo, ct);

        convite.MarcarComoUsado();
        await conviteFuncionarioRepository.AtualizarAsync(convite, ct);
    }

    // Reaplicado tanto na consulta de detalhes (passo de UX, antes do aceite) quanto no
    // aceite de fato — nunca confia que o front só chega ao aceite depois de uma consulta
    // bem-sucedida, mesmo espírito de ObterCodigoValidoOuFalharAsync (AuthService, RF06-A).
    private async Task<ConviteFuncionario> ObterConviteValidoOuFalharAsync(string token, CancellationToken ct)
    {
        var hash = tokenService.HashTokenConvite(token);
        var convite = await conviteFuncionarioRepository.BuscarPorHashAsync(hash, ct);

        if (convite is null || !convite.EstaValido)
        {
            throw new ConviteInvalidoException();
        }

        return convite;
    }

    private static string DescricaoPapel(PapelPredio papel) => papel switch
    {
        PapelPredio.Gestor => "Gestor",
        PapelPredio.Colaborador => "Colaborador",
        _ => papel.ToString(),
    };
}
