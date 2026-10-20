using euSindico.Domain.Entities;

namespace euSindico.Domain.Tests;

public class PlanejamentoTests
{
    [Fact]
    public void Construtor_define_dados_iniciais()
    {
        var antes = DateTime.UtcNow;
        var data = new DateOnly(2026, 12, 1);

        var planejamento = new Planejamento("Pintura da fachada", predioId: 10, data, 15000.50m, "Tintas Ltda.", "Cotar 3 orçamentos");

        Assert.Equal("Pintura da fachada", planejamento.Titulo);
        Assert.Equal(10, planejamento.PredioId);
        Assert.Equal(data, planejamento.DataPlanejamento);
        Assert.Equal(15000.50m, planejamento.OrcamentoPrevisto);
        Assert.Equal("Tintas Ltda.", planejamento.NomePrestadorServico);
        Assert.Equal("Cotar 3 orçamentos", planejamento.Detalhes);
        Assert.InRange(planejamento.CriadoEm, antes, DateTime.UtcNow);
    }

    [Fact]
    public void Construtor_aceita_planejamento_sem_data_e_sem_orcamento_RN11()
    {
        var planejamento = new Planejamento("Trocar o portão", predioId: 10);

        Assert.Null(planejamento.DataPlanejamento);
        Assert.Null(planejamento.OrcamentoPrevisto);
        Assert.Null(planejamento.NomePrestadorServico);
        Assert.Null(planejamento.Detalhes);
    }

    [Fact]
    public void AtualizarDados_altera_os_campos_e_preserva_predio_e_criacao()
    {
        var planejamento = new Planejamento("Pintura", predioId: 10, new DateOnly(2026, 12, 1), 100m, "Atlas", "Antigo");
        var criadoEm = planejamento.CriadoEm;

        planejamento.AtualizarDados("Reforma", null, 0m, null, null);

        Assert.Equal("Reforma", planejamento.Titulo);
        Assert.Null(planejamento.DataPlanejamento);
        Assert.Equal(0m, planejamento.OrcamentoPrevisto);
        Assert.Null(planejamento.NomePrestadorServico);
        Assert.Null(planejamento.Detalhes);
        Assert.Equal(10, planejamento.PredioId);
        Assert.Equal(criadoEm, planejamento.CriadoEm);
    }
}
