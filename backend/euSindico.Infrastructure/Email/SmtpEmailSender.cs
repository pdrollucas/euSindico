using euSindico.Application.Common.Interfaces;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace euSindico.Infrastructure.Email;

public class SmtpEmailSender(IOptions<SmtpOptions> smtpOptions) : IEmailSender
{
    private readonly SmtpOptions _options = smtpOptions.Value;

    public async Task EnviarAsync(string destinatario, string assunto, string corpo, CancellationToken ct = default)
    {
        var mensagem = new MimeMessage();
        mensagem.From.Add(new MailboxAddress(_options.RemetenteNome, _options.RemetenteEmail));
        mensagem.To.Add(MailboxAddress.Parse(destinatario));
        mensagem.Subject = assunto;
        mensagem.Body = new TextPart("plain") { Text = corpo };

        using var client = new SmtpClient();
        await client.ConnectAsync(_options.Host, _options.Port, SecureSocketOptions.StartTls, ct);
        await client.AuthenticateAsync(_options.Usuario, _options.Senha, ct);
        await client.SendAsync(mensagem, ct);
        await client.DisconnectAsync(true, ct);
    }
}