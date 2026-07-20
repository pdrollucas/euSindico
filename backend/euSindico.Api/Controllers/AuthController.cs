using euSindico.Application.Auth;
using euSindico.Application.Auth.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace euSindico.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController(
    AuthService authService,
    IValidator<RegistrarUsuarioDto> registrarValidator,
    IValidator<LoginDto> loginValidator,
    IValidator<RefreshTokenDto> refreshValidator) : ControllerBase
{
    [HttpPost("registrar")]
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
}
