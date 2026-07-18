namespace euSindico.Application.Common.Interfaces;

public interface IPasswordHasher
{
    string Hash(string senha);
}
