using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Application.Auth;
using euSindico.Application.Auth.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace euSindico.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    AuthService authService,
    IValidator<RegistrarUsuarioDto> registrarValidator,
    IValidator<LoginDto> loginValidator,
    IValidator<RefreshTokenDto> refreshValidator,
    IValidator<EsqueciSenhaDto> esqueciSenhaValidator,
    IValidator<VerificarCodigoDto> verificarCodigoValidator,
    IValidator<RedefinirSenhaDto> redefinirSenhaValidator) : ControllerBase
{
    // O id do usuário vem sempre da claim do token (sub), nunca de um parâmetro de rota/query (RN02).
    private int UsuarioId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost("registrar")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Registrar(RegistrarUsuarioDto dto, CancellationToken ct)
    {
        var validationResult = await registrarValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var usuario = await authService.RegistrarAsync(dto, ct);
        return StatusCode(StatusCodes.Status201Created, usuario);
    }

    [HttpPost("login")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginDto dto, CancellationToken ct)
    {
        var validationResult = await loginValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var tokens = await authService.LoginAsync(dto, ct);
        return Ok(tokens);
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(RefreshTokenDto dto, CancellationToken ct)
    {
        var validationResult = await refreshValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var tokens = await authService.RenovarTokenAsync(dto, ct);
        return Ok(tokens);
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(RefreshTokenDto dto, CancellationToken ct)
    {
        var validationResult = await refreshValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await authService.LogoutAsync(UsuarioId, dto, ct);
        return NoContent();
    }

    [HttpPost("esqueci-senha")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> EsqueciSenha(EsqueciSenhaDto dto, CancellationToken ct)
    {
        var validationResult = await esqueciSenhaValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await authService.SolicitarRedefinicaoSenhaAsync(dto, ct);
        return NoContent();
    }

    [HttpPost("verificar-codigo")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> VerificarCodigo(VerificarCodigoDto dto, CancellationToken ct)
    {
        var validationResult = await verificarCodigoValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await authService.VerificarCodigoRedefinicaoAsync(dto, ct);
        return NoContent();
    }

    [HttpPost("redefinir-senha")]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> RedefinirSenha(RedefinirSenhaDto dto, CancellationToken ct)
    {
        var validationResult = await redefinirSenhaValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await authService.RedefinirSenhaAsync(dto, ct);
        return NoContent();
    }
}
