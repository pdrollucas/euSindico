using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Application.Documentos;
using euSindico.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace euSindico.Api.Controllers;

/// <summary>
/// Rotas comuns de atas e normas (RF22–RF25). As telas do front são separadas, então cada tipo tem a
/// sua rota (<see cref="AtaController"/>, <see cref="NormaController"/>), mas o comportamento é o mesmo:
/// a subclasse só fixa o <see cref="Tipo"/> e o prefixo. Ver DOCUMENTOS.md.
/// </summary>
[ApiController]
[Authorize]
public abstract class DocumentoControllerBase(DocumentoService documentoService) : ControllerBase
{
    /// <summary>Nome da política de rate limiting do envio, registrada em <c>Program.cs</c>.</summary>
    public const string PoliticaRateLimitEnvio = "upload";

    private const int PageSizeMaximo = 20;
    private const int NomeFiltroMaximo = 255;

    // 20 MB do arquivo (RN12) + folga para os cabeçalhos do multipart. O limite exato do arquivo é
    // conferido logo abaixo; este só corta, cedo, requisições absurdamente maiores.
    private const long LimiteRequisicaoBytes = ArquivoDocumento.TamanhoMaximoBytes + 1024 * 1024;

    protected abstract TipoDocumentoPredio Tipo { get; }

    // O id do usuário vem sempre da claim do token (sub), nunca de um parâmetro de rota/query (RN02).
    private int UsuarioId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    [EnableRateLimiting(PoliticaRateLimitEnvio)]
    [RequestSizeLimit(LimiteRequisicaoBytes)]
    public async Task<IActionResult> Enviar(int predioId, IFormFile? arquivo, CancellationToken ct)
    {
        if (arquivo is null || arquivo.Length == 0)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { [nameof(arquivo)] = ["O arquivo é obrigatório."] }));
        }

        // Recusa antes de ler o arquivo para a memória.
        if (arquivo.Length > ArquivoDocumento.TamanhoMaximoBytes)
        {
            return ValidationProblem(new ValidationProblemDetails(
                new Dictionary<string, string[]> { [nameof(arquivo)] = [ArquivoDocumento.MensagemTamanho] }));
        }

        using var memoria = new MemoryStream((int)arquivo.Length);
        await arquivo.CopyToAsync(memoria, ct);

        var documento = await documentoService.EnviarAsync(
            UsuarioId, predioId, Tipo, arquivo.FileName, memoria.ToArray(), ct);

        return CreatedAtAction(nameof(Obter), new { predioId, id = documento.Id }, documento);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        int predioId,
        string? nome = null,
        int page = 1,
        int pageSize = 10,
        CancellationToken ct = default)
    {
        var erros = new Dictionary<string, string[]>();

        if (page < 1)
        {
            erros[nameof(page)] = ["A página deve ser no mínimo 1."];
        }

        if (pageSize < 1 || pageSize > PageSizeMaximo)
        {
            erros[nameof(pageSize)] = [$"O tamanho da página deve estar entre 1 e {PageSizeMaximo}."];
        }

        if (nome is { Length: > NomeFiltroMaximo })
        {
            erros[nameof(nome)] = [$"A busca deve ter no máximo {NomeFiltroMaximo} caracteres."];
        }

        if (erros.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(erros));
        }

        var resultado = await documentoService.ListarAsync(UsuarioId, predioId, Tipo, nome, page, pageSize, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int predioId, int id, CancellationToken ct)
    {
        var documento = await documentoService.ObterAsync(UsuarioId, predioId, Tipo, id, ct);
        return Ok(documento);
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Baixar(int predioId, int id, CancellationToken ct)
    {
        var arquivo = await documentoService.BaixarAsync(UsuarioId, predioId, Tipo, id, ct);

        // Sempre como anexo (nome de download informado): o navegador nunca renderiza o arquivo no
        // contexto da API. O FileStreamResult descarta o stream ao final da resposta.
        Response.Headers.XContentTypeOptions = "nosniff";
        return File(arquivo.Conteudo, arquivo.ContentType, arquivo.NomeArquivo);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int predioId, int id, CancellationToken ct)
    {
        await documentoService.ExcluirAsync(UsuarioId, predioId, Tipo, id, ct);
        return NoContent();
    }
}
