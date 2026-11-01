using System.Net;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;

namespace euSindico.Application.Suporte;

public class SuporteService(
    IUsuarioRepository usuarioRepository,
    ISuporteDestino destino,
    IEmailSender emailSender)
{
    public async Task EnviarAsync(int usuarioId, EnviarSuporteDto dto, CancellationToken ct = default)
    {
        var usuario = await usuarioRepository.BuscarPorIdAsync(usuarioId, ct) ?? throw new UsuarioNaoEncontradoException();

        var assunto = $"[Suporte euSíndico] {dto.Assunto}";
        var corpoTexto =
            $"De: {usuario.Nome} <{usuario.Email}> (id {usuario.Id})\n\n{dto.Mensagem}";

        // Tudo que veio do usuário é codificado: o corpo HTML é lido num cliente de e-mail.
        var corpoHtml =
            $"<p><strong>De:</strong> {WebUtility.HtmlEncode(usuario.Nome)} &lt;{WebUtility.HtmlEncode(usuario.Email)}&gt; (id {usuario.Id})</p>" +
            $"<p style=\"white-space: pre-wrap\">{WebUtility.HtmlEncode(dto.Mensagem)}</p>";

        await emailSender.EnviarComRespostaParaAsync(
            destino.EmailDestino, usuario.Email, usuario.Nome, assunto, corpoTexto, corpoHtml, ct);
    }
}
