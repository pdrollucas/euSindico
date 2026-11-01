using euSindico.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace euSindico.Infrastructure.Email;

public class SuporteDestino(IOptions<SuporteOptions> options) : ISuporteDestino
{
    public string EmailDestino => options.Value.EmailDestino;
}
