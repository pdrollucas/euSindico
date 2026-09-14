using euSindico.Application.Equipe;
using euSindico.Application.Equipe.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace euSindico.Api.Controllers;

/// <summary>
/// Consultar e aceitar um convite de funcionário (RF30). Nenhuma rota exige <c>[Authorize]</c> —
/// quem chega aqui, por definição, ainda não tem sessão no sistema (mesmo raciocínio do
/// fluxo de recuperação de senha, RF06-A). Ver EQUIPE.md, Fluxo 2.
/// </summary>
[ApiController]
[Route("convites")]
public class ConviteController(EquipeService equipeService, IValidator<AceitarConviteDto> aceitarValidator) : ControllerBase
{
    [HttpGet("{token}")]
    public async Task<IActionResult> ObterDetalhe(string token, CancellationToken ct)
    {
        var detalhe = await equipeService.ObterDetalheConviteAsync(token, ct);
        return Ok(detalhe);
    }

    [HttpPost("{token}/aceitar")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Aceitar(string token, AceitarConviteDto dto, CancellationToken ct)
    {
        var validationResult = await aceitarValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await equipeService.AceitarConviteAsync(token, dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }
}
