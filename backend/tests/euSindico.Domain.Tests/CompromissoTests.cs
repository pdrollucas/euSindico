using euSindico.Domain.Entities;

namespace euSindico.Domain.Tests;

public class CompromissoTests
{
    private static readonly DateOnly Data = new(2026, 10, 17);
    private static readonly TimeOnly Horario = new(14, 30);

    [Fact]
    public void Construtor_define_dados_iniciais_nao_concluido_e_responsavel()
    {
        var antes = DateTime.UtcNow;
        var compromisso = new Compromisso("Visita técnica", Data, Horario, predioId: 10, responsavelUsuarioId: 2, "Atlas", "Revisar o elevador", 1500.50m);

        Assert.Equal(1500.50m, compromisso.Orcamento);
        Assert.Equal("Visita técnica", compromisso.Titulo);
        Assert.Equal(Data, compromisso.DataCompromisso);
        Assert.Equal(Horario, compromisso.HorarioCompromisso);
        Assert.Equal(10, compromisso.PredioId);
        Assert.Equal(2, compromisso.ResponsavelUsuarioId);
        Assert.Equal("Atlas", compromisso.NomePrestadorServico);
        Assert.Equal("Revisar o elevador", compromisso.Detalhes);
        Assert.False(compromisso.Concluido);
        Assert.InRange(compromisso.CriadoEm, antes, DateTime.UtcNow);
    }

    [Fact]
    public void AtualizarDados_altera_todos_os_campos_inclusive_predio_e_responsavel()
    {
        var compromisso = new Compromisso("Visita", Data, Horario, predioId: 10, responsavelUsuarioId: 2);

        compromisso.AtualizarDados("Reunião", new DateOnly(2026, 11, 1), new TimeOnly(9, 0), "Maria", "Pauta", predioId: 20, responsavelUsuarioId: 3, orcamento: 250m);

        Assert.Equal(250m, compromisso.Orcamento);
        Assert.Equal("Reunião", compromisso.Titulo);
        Assert.Equal(new DateOnly(2026, 11, 1), compromisso.DataCompromisso);
        Assert.Equal(new TimeOnly(9, 0), compromisso.HorarioCompromisso);
        Assert.Equal("Maria", compromisso.NomePrestadorServico);
        Assert.Equal("Pauta", compromisso.Detalhes);
        Assert.Equal(20, compromisso.PredioId);
        Assert.Equal(3, compromisso.ResponsavelUsuarioId);
    }

    [Fact]
    public void Orcamento_e_opcional_e_pode_ser_removido_na_edicao()
    {
        var compromisso = new Compromisso("Visita", Data, Horario, predioId: 10, responsavelUsuarioId: 2, orcamento: 100m);

        compromisso.AtualizarDados("Visita", Data, Horario, null, null, predioId: 10, responsavelUsuarioId: 2, orcamento: null);

        Assert.Null(compromisso.Orcamento);
        Assert.Null(new Compromisso("Visita", Data, Horario, predioId: 10, responsavelUsuarioId: 2).Orcamento);
    }

    [Fact]
    public void Concluir_e_Reabrir_alternam_o_estado()
    {
        var compromisso = new Compromisso("Visita", Data, Horario, predioId: 10, responsavelUsuarioId: 2);

        compromisso.Concluir();
        Assert.True(compromisso.Concluido);

        compromisso.Reabrir();
        Assert.False(compromisso.Concluido);
    }

    [Fact]
    public void Concluir_e_idempotente()
    {
        var compromisso = new Compromisso("Visita", Data, Horario, predioId: 10, responsavelUsuarioId: 2);

        compromisso.Concluir();
        compromisso.Concluir();

        Assert.True(compromisso.Concluido);
    }

    [Fact]
    public void AtribuirResponsavel_troca_somente_o_responsavel()
    {
        var compromisso = new Compromisso("Visita", Data, Horario, predioId: 10, responsavelUsuarioId: 2);

        compromisso.AtribuirResponsavel(1);

        Assert.Equal(1, compromisso.ResponsavelUsuarioId);
        Assert.Equal(10, compromisso.PredioId);
        Assert.Equal("Visita", compromisso.Titulo);
    }
}
