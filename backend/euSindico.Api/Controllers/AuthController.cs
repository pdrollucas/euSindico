using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Application.Auth;
using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
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
    IValidator<EsqueciSenhaDto> esqueciSenhaValidator,
    IValidator<VerificarCodigoDto> verificarCodigoValidator,
    IValidator<RedefinirSenhaDto> redefinirSenhaValidator) : ControllerBase
{
    // Nome do cookie HttpOnly que carrega o refresh token — nunca aparece no corpo JSON
    // (ver SECURITY.md, seção 1, e AUTHENTICATION.md). Restrito a /auth/* via Path abaixo.
    private const string RefreshTokenCookieName = "refreshToken";

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
        DefinirCookieRefreshToken(tokens.RefreshToken, tokens.ExpiraEm);
        return Ok(new AccessTokenResponseDto(tokens.AccessToken));
    }

    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        if (!Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) || string.IsNullOrEmpty(refreshToken))
        {
            throw new RefreshTokenInvalidoException();
        }

        var tokens = await authService.RenovarTokenAsync(new RefreshTokenDto(refreshToken), ct);
        DefinirCookieRefreshToken(tokens.RefreshToken, tokens.ExpiraEm);
        return Ok(new AccessTokenResponseDto(tokens.AccessToken));
    }

    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout(CancellationToken ct)
    {
        // Idempotente: se o cookie não existir, não há sessão pra encerrar no banco — mas o
        // cookie ainda é limpo abaixo, incondicionalmente (ver AUTHENTICATION.md, Fluxo 5).
        if (Request.Cookies.TryGetValue(RefreshTokenCookieName, out var refreshToken) && !string.IsNullOrEmpty(refreshToken))
        {
            await authService.LogoutAsync(UsuarioId, new RefreshTokenDto(refreshToken), ct);
        }

        LimparCookieRefreshToken();
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

    private void DefinirCookieRefreshToken(string refreshToken, DateTime expiraEm)
    {
        Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.None,
            Path = "/auth",
            Expires = new DateTimeOffset(DateTime.SpecifyKind(expiraEm, DateTimeKind.Utc)),
        });
    }

    private void LimparCookieRefreshToken()
    {
        Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions
        {
            HttpOnly = true,
            Path = "/auth",
            Secure = true,
            SameSite = SameSiteMode.None,
        });
    }
}
