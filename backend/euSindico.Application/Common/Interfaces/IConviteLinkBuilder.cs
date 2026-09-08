namespace euSindico.Application.Common.Interfaces;

/// <summary>
/// Monta a URL completa que o convidado recebe por e-mail (RF29) — a Application só conhece
/// o token, nunca o domínio do frontend (isso é configuração de ambiente, resolvida na
/// Infrastructure, mesmo raciocínio de <see cref="IEmailSender"/>/<see cref="ITokenService"/>).
/// </summary>
public interface IConviteLinkBuilder
{
    string Construir(string token);
}
