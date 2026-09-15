using System.Text.Json;
using euSindico.Api.Middleware;
using euSindico.Application.Common.Exceptions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace euSindico.Api.Tests.Middleware;

public class ApplicationExceptionHandlerTests
{
    private readonly ApplicationExceptionHandler _sut = new(NullLogger<ApplicationExceptionHandler>.Instance);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task Excecao_conhecida_retorna_status_e_mensagem_correspondentes()
    {
        var httpContext = CriarHttpContext();
        var exception = new EmailJaCadastradoException("joao@eusindico.com");

        var tratado = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(tratado);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);

        var body = await LerCorpoAsync(httpContext);
        Assert.Equal(StatusCodes.Status409Conflict, body.Status);
        Assert.Equal("O e-mail 'joao@eusindico.com' já está cadastrado.", body.Title);
    }

    [Fact]
    public async Task PredioNaoEncontradoException_retorna_404()
    {
        var httpContext = CriarHttpContext();

        var tratado = await _sut.TryHandleAsync(httpContext, new PredioNaoEncontradoException(), CancellationToken.None);

        Assert.True(tratado);
        Assert.Equal(StatusCodes.Status404NotFound, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task AcaoNaoPermitidaException_retorna_403()
    {
        var httpContext = CriarHttpContext();

        var tratado = await _sut.TryHandleAsync(httpContext, new AcaoNaoPermitidaException(), CancellationToken.None);

        Assert.True(tratado);
        Assert.Equal(StatusCodes.Status403Forbidden, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task ConviteInvalidoException_retorna_400()
    {
        var httpContext = CriarHttpContext();

        var tratado = await _sut.TryHandleAsync(httpContext, new ConviteInvalidoException(), CancellationToken.None);

        Assert.True(tratado);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task EmailJaVinculadoException_retorna_409()
    {
        var httpContext = CriarHttpContext();

        var tratado = await _sut.TryHandleAsync(httpContext, new EmailJaVinculadoException("ana@eusindico.com"), CancellationToken.None);

        Assert.True(tratado);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task PredioDuplicadoException_retorna_409()
    {
        var httpContext = CriarHttpContext();

        var tratado = await _sut.TryHandleAsync(httpContext, new PredioDuplicadoException(), CancellationToken.None);

        Assert.True(tratado);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task PredioLimiteAtingidoException_retorna_409()
    {
        var httpContext = CriarHttpContext();

        var tratado = await _sut.TryHandleAsync(httpContext, new PredioLimiteAtingidoException(), CancellationToken.None);

        Assert.True(tratado);
        Assert.Equal(StatusCodes.Status409Conflict, httpContext.Response.StatusCode);
    }

    [Fact]
    public async Task Excecao_nao_mapeada_retorna_500_com_mensagem_generica_sem_vazar_detalhe_interno()
    {
        var httpContext = CriarHttpContext();
        var exception = new InvalidOperationException("detalhe interno sensível, não deve chegar ao cliente");

        var tratado = await _sut.TryHandleAsync(httpContext, exception, CancellationToken.None);

        Assert.True(tratado);
        Assert.Equal(StatusCodes.Status500InternalServerError, httpContext.Response.StatusCode);

        var body = await LerCorpoAsync(httpContext);
        Assert.Equal(StatusCodes.Status500InternalServerError, body.Status);
        Assert.DoesNotContain("detalhe interno sensível", body.Title);
        Assert.Equal("Ocorreu um erro inesperado. Tente novamente mais tarde.", body.Title);
    }

    private static DefaultHttpContext CriarHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Response.Body = new MemoryStream();
        return httpContext;
    }

    private static async Task<ProblemDetails> LerCorpoAsync(HttpContext httpContext)
    {
        httpContext.Response.Body.Seek(0, SeekOrigin.Begin);
        var problemDetails = await JsonSerializer.DeserializeAsync<ProblemDetails>(
            httpContext.Response.Body,
            JsonOptions);

        return problemDetails ?? throw new InvalidOperationException("Corpo da resposta vazio.");
    }
}
