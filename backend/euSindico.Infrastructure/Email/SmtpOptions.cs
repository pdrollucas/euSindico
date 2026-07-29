namespace euSindico.Infrastructure.Email;

public class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    public string Usuario { get; set; } = string.Empty;
    public string Senha { get; set; } = string.Empty;
    public string RemetenteEmail { get; set; } = string.Empty;
    public string RemetenteNome { get; set; } = "euSíndico";
}