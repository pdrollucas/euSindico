using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Application.Relatorios;
using euSindico.Application.Relatorios.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace euSindico.Api.Controllers;

[ApiController]
[Authorize]
[Route("predios/{predioId:int}/relatorios")]
public class RelatorioController(
    RelatorioService relatorioService,
    IValidator<GerarRelatorioDto> gerarValidator) : ControllerBase
{
    private const int PageSizeMaximo = 20;

    // O id do usuário vem sempre da claim do token (sub), nunca de um parâmetro de rota/query (RN02).
    private int UsuarioId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    public async Task<IActionResult> Gerar(int predioId, GerarRelatorioDto dto, CancellationToken ct)
    {
        var validationResult = await gerarValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var relatorio = await relatorioService.GerarAsync(UsuarioId, predioId, dto.Mes!.Value, dto.Ano!.Value, ct);

        return CreatedAtAction(nameof(Obter), new { predioId, id = relatorio.Id }, relatorio);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        int predioId,
        int? ano = null,
        int? mes = null,
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

        if (mes is < 1 or > 12)
        {
            erros[nameof(mes)] = ["O mês deve estar entre 1 e 12."];
        }

        if (ano is < 1)
        {
            erros[nameof(ano)] = ["O ano deve ser maior que zero."];
        }

        if (erros.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(erros));
        }

        var resultado = await relatorioService.ListarAsync(UsuarioId, predioId, ano, mes, page, pageSize, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int predioId, int id, CancellationToken ct)
    {
        var relatorio = await relatorioService.ObterAsync(UsuarioId, predioId, id, ct);
        return Ok(relatorio);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int predioId, int id, CancellationToken ct)
    {
        await relatorioService.ExcluirAsync(UsuarioId, predioId, id, ct);
        return NoContent();
    }

    [HttpGet("{id:int}/download")]
    public async Task<IActionResult> Baixar(int predioId, int id, CancellationToken ct)
    {
        var arquivo = await relatorioService.BaixarAsync(UsuarioId, predioId, id, ct);

        // O FileStreamResult descarta o stream ao final da resposta.
        return File(arquivo.Conteudo, "application/pdf", arquivo.NomeArquivo);
    }
}
