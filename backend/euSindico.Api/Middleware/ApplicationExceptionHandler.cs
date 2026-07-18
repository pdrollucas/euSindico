using euSindico.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace euSindico.Api.Middleware;

/// <summary>
/// Traduz exceções de negócio da Application para respostas HTTP, num único lugar
/// (ver ARCHITECTURE.md: "tratamento global de erros" é responsabilidade da Api).
///
/// Exceções não mapeadas (bugs, falhas inesperadas) nunca retornam a mensagem real ao
/// cliente — só um texto genérico. O detalhe completo vai para o log (RFC seção 6.1:
/// "tratamento adequado de erros sem exposição de informações sensíveis").
/// </summary>
public class ApplicationExceptionHandler(ILogger<ApplicationExceptionHandler> logger) : IExceptionHandler
{
    private const string MensagemErroGenerico = "Ocorreu um erro inesperado. Tente novamente mais tarde.";

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            EmailJaCadastradoException => StatusCodes.Status409Conflict,
            _ => 0,
        };

        if (statusCode == 0)
        {
            logger.LogError(
                exception,
                "Erro não tratado ao processar {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);

            httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;
            await httpContext.Response.WriteAsJsonAsync(
                new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = MensagemErroGenerico },
                cancellationToken);

            return true;
        }

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(
            new ProblemDetails { Status = statusCode, Title = exception.Message },
            cancellationToken);

        return true;
    }
}
