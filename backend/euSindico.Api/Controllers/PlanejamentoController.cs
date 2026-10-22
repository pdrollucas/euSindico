using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Application.Planejamentos;
using euSindico.Application.Planejamentos.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace euSindico.Api.Controllers;

[ApiController]
[Authorize]
[Route("predios/{predioId:int}/planejamentos")]
public class PlanejamentoController(
    PlanejamentoService planejamentoService,
    IValidator<PlanejamentoFormDto> formValidator) : ControllerBase
{
    private const int PageSizeMaximo = 20;
    private const int TituloFiltroMaximo = 150;

    // O id do usuário vem sempre da claim do token (sub), nunca de um parâmetro de rota/query (RN02).
    private int UsuarioId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    public async Task<IActionResult> Criar(int predioId, PlanejamentoFormDto dto, CancellationToken ct)
    {
        var validationResult = await formValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var planejamento = await planejamentoService.CriarAsync(UsuarioId, predioId, dto, ct);

        return CreatedAtAction(nameof(Obter), new { predioId, id = planejamento.Id }, planejamento);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(
        int predioId,
        string? titulo = null,
        decimal? orcamentoMinimo = null,
        decimal? orcamentoMaximo = null,
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

        if (titulo is { Length: > TituloFiltroMaximo })
        {
            erros[nameof(titulo)] = [$"A busca deve ter no máximo {TituloFiltroMaximo} caracteres."];
        }

        if (orcamentoMinimo is < 0)
        {
            erros[nameof(orcamentoMinimo)] = ["O orçamento mínimo não pode ser negativo."];
        }

        if (orcamentoMaximo is < 0)
        {
            erros[nameof(orcamentoMaximo)] = ["O orçamento máximo não pode ser negativo."];
        }

        if (orcamentoMinimo is { } minimo && orcamentoMaximo is { } maximo && minimo > maximo)
        {
            erros[nameof(orcamentoMinimo)] = ["O orçamento mínimo não pode ser maior que o máximo."];
        }

        if (erros.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(erros));
        }

        var resultado = await planejamentoService.ListarAsync(
            UsuarioId, predioId, titulo, orcamentoMinimo, orcamentoMaximo, page, pageSize, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int predioId, int id, CancellationToken ct)
    {
        var planejamento = await planejamentoService.ObterAsync(UsuarioId, predioId, id, ct);
        return Ok(planejamento);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int predioId, int id, PlanejamentoFormDto dto, CancellationToken ct)
    {
        var validationResult = await formValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var planejamento = await planejamentoService.AtualizarAsync(UsuarioId, predioId, id, dto, ct);
        return Ok(planejamento);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int predioId, int id, CancellationToken ct)
    {
        await planejamentoService.ExcluirAsync(UsuarioId, predioId, id, ct);
        return NoContent();
    }
}
