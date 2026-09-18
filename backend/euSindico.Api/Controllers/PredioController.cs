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
public class PredioController(PredioService predioService, IValidator<CriarPredioDto> criarValidator) : ControllerBase
{
    // O id do usuário vem sempre da claim do token (sub), nunca de um parâmetro de rota/query (RN02).
    private int UsuarioId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    public async Task<IActionResult> Criar(CriarPredioDto dto, CancellationToken ct)
    {
        var validationResult = await criarValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var predio = await predioService.CriarAsync(UsuarioId, dto, ct);

        return CreatedAtAction(nameof(Obter), new { id = predio.Id }, predio);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id, CancellationToken ct)
    {
        var predio = await predioService.ObterAsync(UsuarioId, id, ct);
        return Ok(predio);
    }
}
