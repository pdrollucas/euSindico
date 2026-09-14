using euSindico.Application.Common.Interfaces;
using Microsoft.Extensions.Options;

namespace euSindico.Infrastructure.Web;

public class ConviteLinkBuilder(IOptions<FrontendOptions> options) : IConviteLinkBuilder
{
    private readonly FrontendOptions _options = options.Value;

    public string Construir(string token) => $"{_options.BaseUrl.TrimEnd('/')}/convite/{token}";
}
