using euSindico.Infrastructure.Time;

namespace euSindico.Infrastructure.Tests.Time;

public class FusoHorarioTimeProviderTests
{
    [Fact]
    public void GetLocalNow_usa_o_fuso_configurado_e_nao_o_do_servidor()
    {
        var fuso = TimeZoneInfo.CreateCustomTimeZone("Teste", TimeSpan.FromHours(-3), "Teste", "Teste");
        var sut = new FusoHorarioTimeProvider(fuso);

        var local = sut.GetLocalNow();

        Assert.Equal(TimeSpan.FromHours(-3), local.Offset);
        Assert.Same(fuso, sut.LocalTimeZone);
    }

    [Fact]
    public void Fuso_padrao_e_America_Sao_Paulo_e_existe_no_sistema()
    {
        var fuso = TimeZoneInfo.FindSystemTimeZoneById(FusoHorarioTimeProvider.FusoPadrao);

        Assert.Equal(TimeSpan.FromHours(-3), fuso.BaseUtcOffset);
    }
}
