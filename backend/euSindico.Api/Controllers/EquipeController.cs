using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Application.Equipe;
using euSindico.Application.Equipe.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace euSindico.Api.Controllers;

/// <summary>
/// Convidar, listar e remover membros da equipe de um prédio (RF29, RF31, RF32). Todas as
/// ações exigem que quem chama seja o Síndico do prédio — checado dentro do
/// <see cref="EquipeService"/> via <see cref="AutorizacaoPredioService"/>, nunca aqui no
/// Controller (ver EQUIPE.md).
/// </summary>
[ApiController]
[Authorize]
[Route("predios/{predioId:int}")]
public class EquipeController(EquipeService equipeService, IValidator<ConvidarFuncionarioDto> convidarValidator) : ControllerBase
{
    // O id do usuário vem sempre da claim do token (sub), nunca de um parâmetro de rota/query (RN02).
    private int UsuarioId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost("convites")]
    public async Task<IActionResult> Convidar(int predioId, ConvidarFuncionarioDto dto, CancellationToken ct)
    {
        var validationResult = await convidarValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await equipeService.ConvidarAsync(UsuarioId, predioId, dto, ct);
        return StatusCode(StatusCodes.Status201Created);
    }

    [HttpGet("equipe")]
    public async Task<IActionResult> ListarEquipe(int predioId, CancellationToken ct)
    {
        var membros = await equipeService.ListarEquipeAsync(UsuarioId, predioId, ct);
        return Ok(membros);
    }

    [HttpDelete("equipe/{usuarioAlvoId:int}")]
    public async Task<IActionResult> RemoverMembro(int predioId, int usuarioAlvoId, CancellationToken ct)
    {
        await equipeService.RemoverAsync(UsuarioId, predioId, usuarioAlvoId, ct);
        return NoContent();
    }
}
