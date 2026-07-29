namespace euSindico.Application.Auth.Dtos;

/// <summary>
/// Corpo de resposta de login/refresh exposto ao cliente — só o access token. O refresh
/// token nunca aparece no JSON, vai exclusivamente via cookie <c>HttpOnly</c> (ver
/// <c>AuthController</c> e SECURITY.md, seção 1).
/// </summary>
public record AccessTokenResponseDto(string AccessToken);
