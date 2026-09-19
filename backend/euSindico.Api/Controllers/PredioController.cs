using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Application.Predios;
using euSindico.Application.Predios.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace euSindico.Api.Controllers;

[ApiController]
[Authorize]
[Route("predios")]
public class PredioController(PredioService predioService, IValidator<PredioFormDto> formValidator) : ControllerBase
{
    private const int PageSizeMaximo = 20;

    // O id do usuário vem sempre da claim do token (sub), nunca de um parâmetro de rota/query (RN02).
    private int UsuarioId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    public async Task<IActionResult> Criar(PredioFormDto dto, CancellationToken ct)
    {
        var validationResult = await formValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var predio = await predioService.CriarAsync(UsuarioId, dto, ct);

        return CreatedAtAction(nameof(Obter), new { id = predio.Id }, predio);
    }

    [HttpGet]
    public async Task<IActionResult> Listar(int page = 1, int pageSize = 10, CancellationToken ct = default)
    {
        if (page < 1 || pageSize < 1 || pageSize > PageSizeMaximo)
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

            return ValidationProblem(new ValidationProblemDetails(erros));
        }

        var resultado = await predioService.ListarAsync(UsuarioId, page, pageSize, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id, CancellationToken ct)
    {
        var predio = await predioService.ObterAsync(UsuarioId, id, ct);
        return Ok(predio);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, PredioFormDto dto, CancellationToken ct)
    {
        var validationResult = await formValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var predio = await predioService.AtualizarAsync(UsuarioId, id, dto, ct);
        return Ok(predio);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        await predioService.ExcluirAsync(UsuarioId, id, ct);
        return NoContent();
    }
}
