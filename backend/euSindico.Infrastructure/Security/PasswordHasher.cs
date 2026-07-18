using euSindico.Application.Common.Interfaces;

namespace euSindico.Infrastructure.Security;

public class PasswordHasher : IPasswordHasher
{
    public string Hash(string senha) => BCrypt.Net.BCrypt.HashPassword(senha);
}
