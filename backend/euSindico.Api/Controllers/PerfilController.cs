using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Application.Auth;
using euSindico.Application.Auth.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace euSindico.Api.Controllers;

[ApiController]
[Route("perfil")]
[Authorize]
public class PerfilController(
    PerfilService perfilService,
    IValidator<AtualizarPerfilDto> atualizarValidator,
    IValidator<AlterarSenhaDto> alterarSenhaValidator) : ControllerBase
{
    // O id do usuário vem sempre da claim do token (sub), nunca de um parâmetro de rota/query —
    // do contrário um usuário poderia acessar dados de outro só trocando um id na URL (RN02).
    private int UsuarioId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpGet]
    public async Task<IActionResult> ObterPerfil(CancellationToken ct)
    {
        var usuario = await perfilService.ObterPerfilAsync(UsuarioId, ct);
        return Ok(usuario);
    }

    [HttpPut]
    public async Task<IActionResult> AtualizarPerfil(AtualizarPerfilDto dto, CancellationToken ct)
    {
        var validationResult = await atualizarValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var usuario = await perfilService.AtualizarPerfilAsync(UsuarioId, dto, ct);
        return Ok(usuario);
    }

    [HttpPut("senha")]
    public async Task<IActionResult> AlterarSenha(AlterarSenhaDto dto, CancellationToken ct)
    {
        var validationResult = await alterarSenhaValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        await perfilService.AlterarSenhaAsync(UsuarioId, dto, ct);
        return NoContent();
    }

    [HttpDelete]
    public async Task<IActionResult> ExcluirConta(CancellationToken ct)
    {
        await perfilService.ExcluirContaAsync(UsuarioId, ct);
        return NoContent();
    }
}
