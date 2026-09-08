namespace euSindico.Application.Common.Interfaces;

public interface IEmailSender
{
    /// <summary>
    /// <paramref name="corpoTexto"/> é sempre enviado (fallback para clientes de e-mail sem
    /// suporte a HTML). <paramref name="corpoHtml"/> é opcional — quando informado, o e-mail
    /// vira <c>multipart/alternative</c> (HTML + texto); a ordem dos parâmetros preserva
    /// todas as chamadas existentes (só texto) sem precisar de argumento nomeado.
    /// </summary>
    Task EnviarAsync(string destinatario, string assunto, string corpoTexto, CancellationToken ct = default, string? corpoHtml = null);
}