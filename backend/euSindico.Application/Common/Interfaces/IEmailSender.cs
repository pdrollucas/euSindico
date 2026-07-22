namespace euSindico.Application.Common.Interfaces;

public interface IEmailSender
{
    Task EnviarAsync(string destinatario, string assunto, string corpo, CancellationToken ct = default);
}