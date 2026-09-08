using euSindico.Domain.Entities;
using euSindico.Domain.Enums;

namespace euSindico.Domain.Tests;

public class ConviteFuncionarioTests
{
    [Fact]
    public void Constructor_define_campos_corretamente()
    {
        var expiraEm = DateTime.UtcNow.AddDays(7);

        var convite = new ConviteFuncionario(
            predioId: 1, email: "ana@eusindico.com", PapelPredio.Gestor, tokenHash: "hash-fake",
            criadoPorUsuarioId: 10, expiraEm);

        Assert.Equal(1, convite.PredioId);
        Assert.Equal("ana@eusindico.com", convite.Email);
        Assert.Equal(PapelPredio.Gestor, convite.Papel);
        Assert.Equal("hash-fake", convite.TokenHash);
        Assert.Equal(10, convite.CriadoPorUsuarioId);
        Assert.Equal(expiraEm, convite.ExpiraEm);
        Assert.Null(convite.UsadoEm);
    }

    [Fact]
    public void Constructor_com_papel_sindico_lanca_excecao()
    {
        Assert.Throws<ArgumentException>(() => new ConviteFuncionario(
            predioId: 1, email: "ana@eusindico.com", PapelPredio.Sindico, tokenHash: "hash-fake",
            criadoPorUsuarioId: 10, DateTime.UtcNow.AddDays(7)));
    }

    [Fact]
    public void EstaValido_com_convite_novo_e_nao_usado_retorna_true()
    {
        var convite = new ConviteFuncionario(
            1, "ana@eusindico.com", PapelPredio.Colaborador, "hash-fake", 10, DateTime.UtcNow.AddDays(7));

        Assert.True(convite.EstaValido);
    }

    [Fact]
    public void EstaValido_com_convite_expirado_retorna_false()
    {
        var convite = new ConviteFuncionario(
            1, "ana@eusindico.com", PapelPredio.Colaborador, "hash-fake", 10, DateTime.UtcNow.AddDays(-1));

        Assert.False(convite.EstaValido);
    }

    [Fact]
    public void EstaValido_com_convite_ja_usado_retorna_false()
    {
        var convite = new ConviteFuncionario(
            1, "ana@eusindico.com", PapelPredio.Colaborador, "hash-fake", 10, DateTime.UtcNow.AddDays(7));

        convite.MarcarComoUsado();

        Assert.False(convite.EstaValido);
    }

    [Fact]
    public void MarcarComoUsado_preenche_usadoEm()
    {
        var convite = new ConviteFuncionario(
            1, "ana@eusindico.com", PapelPredio.Colaborador, "hash-fake", 10, DateTime.UtcNow.AddDays(7));

        convite.MarcarComoUsado();

        Assert.NotNull(convite.UsadoEm);
    }
}
