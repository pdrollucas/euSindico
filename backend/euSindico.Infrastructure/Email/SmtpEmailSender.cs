using euSindico.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace euSindico.Infrastructure.Email;

public class SmtpEmailSender(IOptions<SmtpOptions> smtpOptions) : IEmailSender
{
    private readonly SmtpOptions _options = smtpOptions.Value;

    public async Task EnviarAsync(string destinatario, string assunto, string corpoTexto, CancellationToken ct = default, string? corpoHtml = null)
    {
        var mensagem = MontarMensagem(destinatario, assunto, corpoTexto, corpoHtml);

        await EnviarMensagemAsync(mensagem, ct);
    }

    public async Task EnviarComRespostaParaAsync(
        string destinatario, string replyToEmail, string replyToNome,
        string assunto, string corpoTexto, string corpoHtml, CancellationToken ct)
    {
        var mensagem = MontarMensagem(destinatario, assunto, corpoTexto, corpoHtml);
        mensagem.ReplyTo.Add(new MailboxAddress(replyToNome, replyToEmail));

        await EnviarMensagemAsync(mensagem, ct);
    }

    private MimeMessage MontarMensagem(string destinatario, string assunto, string corpoTexto, string? corpoHtml)
    {
        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(_options.RemetenteNome, _options.RemetenteEmail));
        mensagem.To.Add(MailboxAddress.Parse(destinatario));
        mensagem.Subject = assunto;

        // BodyBuilder monta multipart/alternative automaticamente quando os dois corpos são
        // informados (cliente de e-mail escolhe HTML se suportar, cai para texto senão) —
        // sem HtmlBody, vira só TextPart("plain"), igual ao comportamento anterior (RF06-A).
        var builder = new BodyBuilder { TextBody = corpoTexto };
        if (corpoHtml is not null)
        {
            builder.HtmlBody = corpoHtml;
        }

        mensagem.Body = builder.ToMessageBody();

        return mensagem;
    }

    private async Task EnviarMensagemAsync(MimeMessage mensagem, CancellationToken ct)
    {
        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_options.Usuario, _options.Senha, ct);
        await client.SendAsync(mensagem, ct);
        await client.DisconnectAsync(true, ct);
    }
}
