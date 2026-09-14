using euSindico.Infrastructure.Web;
using Microsoft.Extensions.Options;

namespace euSindico.Infrastructure.Tests.Web;

public class ConviteLinkBuilderTests
{
    [Fact]
    public void Construir_monta_url_com_base_url_e_token()
    {
        var sut = new ConviteLinkBuilder(Options.Create(new FrontendOptions { BaseUrl = "http://localhost:5173" }));

        var link = sut.Construir("token-plano");

        Assert.Equal("http://localhost:5173/convite/token-plano", link);
    }

    [Fact]
    public void Construir_remove_barra_final_do_base_url_para_nao_duplicar()
    {
        var sut = new ConviteLinkBuilder(Options.Create(new FrontendOptions { BaseUrl = "http://localhost:5173/" }));

        var link = sut.Construir("token-plano");

        Assert.Equal("http://localhost:5173/convite/token-plano", link);
    }
}
