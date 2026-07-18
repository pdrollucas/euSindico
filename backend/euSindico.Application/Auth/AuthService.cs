using euSindico.Application.Auth.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Domain.Entities;

namespace euSindico.Application.Auth;

public class AuthService(IUsuarioRepository usuarioRepository, IPasswordHasher passwordHasher)
{
    public async Task<UsuarioDto> RegistrarAsync(RegistrarUsuarioDto dto, CancellationToken ct = default)
    {
        if (await usuarioRepository.ExisteEmailAsync(dto.Email, ct))
        {
            throw new EmailJaCadastradoException(dto.Email);
        }

        var senhaHash = passwordHasher.Hash(dto.Senha);
        var usuario = new Usuario(dto.Nome, dto.Email, senhaHash);

        await usuarioRepository.AdicionarAsync(usuario, ct);

        return new UsuarioDto(usuario.Id, usuario.Nome, usuario.Email, usuario.CriadoEm);
    }
}
