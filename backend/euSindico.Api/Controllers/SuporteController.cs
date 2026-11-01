using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Application.Suporte;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace euSindico.Api.Controllers;

[ApiController]
[Route("suporte")]
[Authorize]
public class SuporteController(
    SuporteService suporteService,
    IValidator<EnviarSuporteDto> validator) : ControllerBase
{
    /// <summary>Nome da política de rate limiting do envio, registrada em <c>Program.cs</c>.</summary>
    public const string PoliticaRateLimitEnvio = "suporte";

    // O id do usuário vem sempre da claim do token (sub) (RN02).
    private int UsuarioId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    [EnableRateLimiting(PoliticaRateLimitEnvio)]
    public async Task<IActionResult> Enviar(EnviarSuporteDto dto, CancellationToken ct)
    {
        var validationResult = await validator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await suporteService.EnviarAsync(UsuarioId, dto, ct);
        return NoContent();
    }
}
