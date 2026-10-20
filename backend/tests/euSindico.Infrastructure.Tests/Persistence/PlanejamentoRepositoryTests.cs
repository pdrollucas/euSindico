using euSindico.Application.Planejamentos;
using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using euSindico.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Tests.Persistence;

/// <summary>
/// Roda o <see cref="PlanejamentoRepository"/> contra SQLite em memória — valida a ordenação
/// (data decrescente, sem data no fim), o filtro por faixa de orçamento e a exclusão de conta com
/// FKs ativas. Não substitui um teste contra MySQL (a tradução de <c>Contains</c> difere em
/// maiúsculas/minúsculas entre os provedores).
/// </summary>
public class PlanejamentoRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly PlanejamentoRepository _sut;

    private readonly Usuario _sindico = new("Sindico", "s@x.com", "hash");
    private readonly Predio _predio;
    private readonly Predio _outroPredio;
    private readonly Predio _predioExcluido;

    // Instantes de criação controlados: o construtor da entidade usa DateTime.UtcNow.
    private DateTime _criadoEm = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public PlanejamentoRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
        _sut = new PlanejamentoRepository(_context);

        _context.Usuarios.Add(_sindico);
        _context.SaveChanges();

        _predio = new Predio("Predio A", "Rua A, 1", _sindico.Id);
        _outroPredio = new Predio("Predio B", "Rua B, 1", _sindico.Id);
        _predioExcluido = new Predio("Predio Excluido", "Rua C, 1", _sindico.Id);
        _predioExcluido.ExcluirLogicamente();
        _context.Predios.AddRange(_predio, _outroPredio, _predioExcluido);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // ---------- Ordenação ----------

    [Fact]
    public async Task Ordena_por_data_decrescente_com_os_sem_data_no_fim_por_criacao_decrescente()
    {
        var semDataAntigo = Adicionar("Sem data antigo", null);
        var dezembro = Adicionar("Dezembro", new DateOnly(2026, 12, 1));
        var semDataRecente = Adicionar("Sem data recente", null);
        var novembro = Adicionar("Novembro", new DateOnly(2026, 11, 1));
        var janeiro = Adicionar("Janeiro", new DateOnly(2027, 1, 1));

        var (itens, total) = await _sut.ListarAsync(Filtro(), 1, 10);

        Assert.Equal(5, total);
        Assert.Equal(
            new[] { janeiro.Id, dezembro.Id, novembro.Id, semDataRecente.Id, semDataAntigo.Id },
            itens.Select(p => p.Id));
    }

    [Fact]
    public async Task Na_mesma_data_o_cadastrado_mais_recentemente_vem_primeiro()
    {
        var data = new DateOnly(2026, 12, 1);
        var antigo = Adicionar("Antigo", data);
        var recente = Adicionar("Recente", data);

        var (itens, _) = await _sut.ListarAsync(Filtro(), 1, 10);

        Assert.Equal(new[] { recente.Id, antigo.Id }, itens.Select(p => p.Id));
    }

    [Fact]
    public async Task Paginacao_respeita_a_ordem_e_informa_o_total()
    {
        var a = Adicionar("A", new DateOnly(2026, 12, 3));
        var b = Adicionar("B", new DateOnly(2026, 12, 2));
        var c = Adicionar("C", new DateOnly(2026, 12, 1));

        var (primeira, total) = await _sut.ListarAsync(Filtro(), 1, 2);
        var (segunda, _) = await _sut.ListarAsync(Filtro(), 2, 2);

        Assert.Equal(3, total);
        Assert.Equal(new[] { a.Id, b.Id }, primeira.Select(p => p.Id));
        Assert.Equal(new[] { c.Id }, segunda.Select(p => p.Id));
    }

    // ---------- Escopo do prédio ----------

    [Fact]
    public async Task Lista_so_os_planejamentos_do_predio_pedido()
    {
        var doPredio = Adicionar("Do prédio A", null);
        Adicionar("Do prédio B", null, predio: _outroPredio);

        var (itens, total) = await _sut.ListarAsync(Filtro(), 1, 10);

        Assert.Equal(1, total);
        Assert.Equal(doPredio.Id, Assert.Single(itens).Id);
    }

    [Fact]
    public async Task Predio_excluido_nao_devolve_planejamentos_RN08()
    {
        Adicionar("Do excluído", null, predio: _predioExcluido);

        var (itens, total) = await _sut.ListarAsync(Filtro(predio: _predioExcluido), 1, 10);

        Assert.Equal(0, total);
        Assert.Empty(itens);
    }

    // ---------- Filtros ----------

    [Fact]
    public async Task Filtra_por_trecho_do_titulo()
    {
        var elevador = Adicionar("Troca do elevador", null);
        Adicionar("Pintura", null);

        var (itens, total) = await _sut.ListarAsync(Filtro(titulo: "elevador"), 1, 10);

        Assert.Equal(1, total);
        Assert.Equal(elevador.Id, Assert.Single(itens).Id);
    }

    [Fact]
    public async Task Faixa_de_orcamento_e_inclusiva_nas_duas_pontas_e_ignora_os_sem_orcamento()
    {
        Adicionar("Abaixo", null, orcamento: 99.99m);
        var noMinimo = Adicionar("No mínimo", null, orcamento: 100m);
        var meio = Adicionar("Meio", null, orcamento: 250m);
        var noMaximo = Adicionar("No máximo", null, orcamento: 500m);
        Adicionar("Acima", null, orcamento: 500.01m);
        Adicionar("Sem orçamento", null);

        var (itens, total) = await _sut.ListarAsync(Filtro(minimo: 100m, maximo: 500m), 1, 10);

        Assert.Equal(3, total);
        Assert.Equivalent(new[] { noMinimo.Id, meio.Id, noMaximo.Id }, itens.Select(p => p.Id));
    }

    [Fact]
    public async Task So_minimo_ou_so_maximo_funcionam_isoladamente()
    {
        var baixo = Adicionar("Baixo", null, orcamento: 10m);
        var alto = Adicionar("Alto", null, orcamento: 1000m);

        var (soMinimo, _) = await _sut.ListarAsync(Filtro(minimo: 500m), 1, 10);
        var (soMaximo, _) = await _sut.ListarAsync(Filtro(maximo: 500m), 1, 10);

        Assert.Equal(alto.Id, Assert.Single(soMinimo).Id);
        Assert.Equal(baixo.Id, Assert.Single(soMaximo).Id);
    }

    [Fact]
    public async Task Orcamento_zero_entra_na_faixa_que_o_inclui()
    {
        var gratis = Adicionar("Sem custo", null, orcamento: 0m);

        var (itens, _) = await _sut.ListarAsync(Filtro(minimo: 0m, maximo: 0m), 1, 10);

        Assert.Equal(gratis.Id, Assert.Single(itens).Id);
    }

    // ---------- Busca, atualização e remoção ----------

    [Fact]
    public async Task BuscarPorIdAsync_carrega_o_predio()
    {
        var planejamento = Adicionar("Pintura", null);
        _context.ChangeTracker.Clear();

        var encontrado = await _sut.BuscarPorIdAsync(planejamento.Id);

        Assert.Equal("Predio A", encontrado!.Predio!.Nome);
    }

    [Fact]
    public async Task BuscarPorIdAsync_inexistente_devolve_nulo() =>
        Assert.Null(await _sut.BuscarPorIdAsync(9999));

    [Fact]
    public async Task AtualizarAsync_persiste_as_alteracoes_inclusive_limpar_a_data()
    {
        var planejamento = Adicionar("Pintura", new DateOnly(2026, 12, 1), orcamento: 100m);

        planejamento.AtualizarDados("Reforma", null, 0m, "Atlas", "Detalhes");
        await _sut.AtualizarAsync(planejamento);
        _context.ChangeTracker.Clear();

        var salvo = await _context.Planejamentos.SingleAsync(p => p.Id == planejamento.Id);
        Assert.Equal("Reforma", salvo.Titulo);
        Assert.Null(salvo.DataPlanejamento);
        Assert.Equal(0m, salvo.OrcamentoPrevisto);
        Assert.Equal("Atlas", salvo.NomePrestadorServico);
    }

    [Fact]
    public async Task RemoverAsync_exclui_fisicamente_RN25()
    {
        var planejamento = Adicionar("Pintura", null);

        await _sut.RemoverAsync(planejamento);

        Assert.False(await _context.Planejamentos.AnyAsync(p => p.Id == planejamento.Id));
    }

    [Fact]
    public async Task Excluir_conta_do_sindico_remove_os_planejamentos_dos_predios_dele_com_FKs_ativas()
    {
        Adicionar("Do A", null);
        Adicionar("Do B", null, predio: _outroPredio);
        var sindicoId = _sindico.Id;

        await new UsuarioRepository(_context).ExcluirUsuarioEDadosRelacionadosAsync(sindicoId);
        _context.ChangeTracker.Clear();

        Assert.False(await _context.Usuarios.AnyAsync(u => u.Id == sindicoId));
        Assert.False(await _context.Planejamentos.AnyAsync());
    }

    // ---------- Auxiliares ----------

    private Planejamento Adicionar(string titulo, DateOnly? data, Predio? predio = null, decimal? orcamento = null)
    {
        var planejamento = new Planejamento(titulo, (predio ?? _predio).Id, data, orcamento);
        _criadoEm = _criadoEm.AddMinutes(1);
        typeof(Planejamento).GetProperty(nameof(Planejamento.CriadoEm))!.SetValue(planejamento, _criadoEm);

        _context.Planejamentos.Add(planejamento);
        _context.SaveChanges();
        return planejamento;
    }

    private PlanejamentoFiltro Filtro(
        Predio? predio = null, string? titulo = null, decimal? minimo = null, decimal? maximo = null) =>
        new((predio ?? _predio).Id, titulo, minimo, maximo);
}
