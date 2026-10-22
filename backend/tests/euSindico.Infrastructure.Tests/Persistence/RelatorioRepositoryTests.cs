using euSindico.Domain.Entities;
using euSindico.Infrastructure.Persistence;
using euSindico.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Tests.Persistence;

/// <summary>
/// Roda <see cref="RelatorioRepository"/> e <see cref="CompromissoRepository.ListarConcluidosDoMesAsync"/>
/// contra SQLite em memória.
/// </summary>
public class RelatorioRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly RelatorioRepository _sut;
    private readonly CompromissoRepository _compromissoRepository;

    private readonly Usuario _sindico = new("Sindico", "s@x.com", "hash");
    private readonly Predio _predio;
    private readonly Predio _outroPredio;
    private readonly Predio _predioExcluido;

    public RelatorioRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
        _sut = new RelatorioRepository(_context);
        _compromissoRepository = new CompromissoRepository(_context);

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

    // ---------- Relatórios ----------

    [Fact]
    public async Task Lista_do_gerado_mais_recentemente_para_o_mais_antigo()
    {
        var primeiro = await AdicionarRelatorio(9, 2026);
        var segundo = await AdicionarRelatorio(9, 2026);
        var terceiro = await AdicionarRelatorio(8, 2026);

        var (itens, total) = await _sut.ListarAsync(_predio.Id, null, null, 1, 10);

        Assert.Equal(3, total);
        Assert.Equal(new[] { terceiro.Id, segundo.Id, primeiro.Id }, itens.Select(r => r.Id));
    }

    [Fact]
    public async Task Filtra_por_ano_e_mes()
    {
        await AdicionarRelatorio(8, 2026);
        var alvo = await AdicionarRelatorio(9, 2026);
        await AdicionarRelatorio(9, 2025);

        var (porMes, _) = await _sut.ListarAsync(_predio.Id, 2026, 9, 1, 10);
        var (porAno, totalAno) = await _sut.ListarAsync(_predio.Id, 2026, null, 1, 10);

        Assert.Equal(alvo.Id, Assert.Single(porMes).Id);
        Assert.Equal(2, totalAno);
        Assert.All(porAno, r => Assert.Equal(2026, r.AnoReferencia));
    }

    [Fact]
    public async Task Paginacao_informa_o_total_e_respeita_a_ordem()
    {
        var a = await AdicionarRelatorio(1, 2026);
        var b = await AdicionarRelatorio(2, 2026);
        var c = await AdicionarRelatorio(3, 2026);

        var (primeira, total) = await _sut.ListarAsync(_predio.Id, null, null, 1, 2);
        var (segunda, _) = await _sut.ListarAsync(_predio.Id, null, null, 2, 2);

        Assert.Equal(3, total);
        Assert.Equal(new[] { c.Id, b.Id }, primeira.Select(r => r.Id));
        Assert.Equal(new[] { a.Id }, segunda.Select(r => r.Id));
    }

    [Fact]
    public async Task Lista_so_os_relatorios_do_predio_pedido()
    {
        var doPredio = await AdicionarRelatorio(9, 2026);
        await AdicionarRelatorio(9, 2026, _outroPredio);

        var (itens, total) = await _sut.ListarAsync(_predio.Id, null, null, 1, 10);

        Assert.Equal(1, total);
        Assert.Equal(doPredio.Id, Assert.Single(itens).Id);
    }

    [Fact]
    public async Task Predio_excluido_nao_devolve_relatorios_RN08()
    {
        await AdicionarRelatorio(9, 2026, _predioExcluido);

        var (itens, total) = await _sut.ListarAsync(_predioExcluido.Id, null, null, 1, 10);

        Assert.Empty(itens);
        Assert.Equal(0, total);
    }

    [Fact]
    public async Task BuscarPorId_carrega_o_predio()
    {
        var relatorio = await AdicionarRelatorio(9, 2026);
        _context.ChangeTracker.Clear();

        var encontrado = await _sut.BuscarPorIdAsync(relatorio.Id);

        Assert.Equal("Predio A", encontrado!.Predio!.Nome);
    }

    [Fact]
    public async Task BuscarPorId_inexistente_devolve_null()
    {
        Assert.Null(await _sut.BuscarPorIdAsync(999));
    }

    [Fact]
    public async Task Contar_considera_so_predio_ano_e_mes_pedidos()
    {
        await AdicionarRelatorio(9, 2026);
        await AdicionarRelatorio(9, 2026);
        await AdicionarRelatorio(8, 2026);
        await AdicionarRelatorio(9, 2025);
        await AdicionarRelatorio(9, 2026, _outroPredio);

        Assert.Equal(2, await _sut.ContarAsync(_predio.Id, 2026, 9));
    }

    [Fact]
    public async Task Remover_apaga_o_registro_e_libera_a_contagem()
    {
        var relatorio = await AdicionarRelatorio(9, 2026);

        await _sut.RemoverAsync(relatorio);

        Assert.Null(await _sut.BuscarPorIdAsync(relatorio.Id));
        Assert.Equal(0, await _sut.ContarAsync(_predio.Id, 2026, 9));
    }

    // ---------- Compromissos concluídos do mês ----------

    [Fact]
    public async Task ListarConcluidosDoMes_traz_so_concluidos_do_mes_em_ordem_cronologica()
    {
        var tarde = AdicionarCompromisso("Tarde", new DateOnly(2026, 9, 10), new TimeOnly(15, 0), concluido: true);
        var manha = AdicionarCompromisso("Manhã", new DateOnly(2026, 9, 10), new TimeOnly(8, 0), concluido: true);
        var primeiroDia = AdicionarCompromisso("Primeiro dia", new DateOnly(2026, 9, 1), new TimeOnly(23, 59), concluido: true);
        var ultimoDia = AdicionarCompromisso("Último dia", new DateOnly(2026, 9, 30), new TimeOnly(0, 0), concluido: true);
        AdicionarCompromisso("Pendente", new DateOnly(2026, 9, 5), new TimeOnly(9, 0), concluido: false);
        AdicionarCompromisso("Mês anterior", new DateOnly(2026, 8, 31), new TimeOnly(9, 0), concluido: true);
        AdicionarCompromisso("Mês seguinte", new DateOnly(2026, 10, 1), new TimeOnly(9, 0), concluido: true);
        AdicionarCompromisso("Ano anterior", new DateOnly(2025, 9, 10), new TimeOnly(9, 0), concluido: true);
        AdicionarCompromisso("Outro prédio", new DateOnly(2026, 9, 10), new TimeOnly(9, 0), concluido: true, predio: _outroPredio);

        var itens = await _compromissoRepository.ListarConcluidosDoMesAsync(_predio.Id, 2026, 9);

        Assert.Equal(
            new[] { primeiroDia.Id, manha.Id, tarde.Id, ultimoDia.Id },
            itens.Select(c => c.Id));
    }

    [Fact]
    public async Task ListarConcluidosDoMes_carrega_o_responsavel()
    {
        AdicionarCompromisso("Visita", new DateOnly(2026, 9, 10), new TimeOnly(8, 0), concluido: true);

        var itens = await _compromissoRepository.ListarConcluidosDoMesAsync(_predio.Id, 2026, 9);

        Assert.Equal("Sindico", Assert.Single(itens).Responsavel!.Nome);
    }

    [Fact]
    public async Task ListarConcluidosDoMes_de_dezembro_nao_vaza_para_janeiro()
    {
        var dezembro = AdicionarCompromisso("Dezembro", new DateOnly(2026, 12, 31), new TimeOnly(9, 0), concluido: true);
        AdicionarCompromisso("Janeiro", new DateOnly(2027, 1, 1), new TimeOnly(9, 0), concluido: true);

        var itens = await _compromissoRepository.ListarConcluidosDoMesAsync(_predio.Id, 2026, 12);

        Assert.Equal(dezembro.Id, Assert.Single(itens).Id);
    }

    [Fact]
    public async Task ListarConcluidosDoMes_ignora_predio_excluido()
    {
        AdicionarCompromisso("Do excluído", new DateOnly(2026, 9, 10), new TimeOnly(8, 0), concluido: true, predio: _predioExcluido);

        var itens = await _compromissoRepository.ListarConcluidosDoMesAsync(_predioExcluido.Id, 2026, 9);

        Assert.Empty(itens);
    }

    // ---------- Auxiliares ----------

    private async Task<Relatorio> AdicionarRelatorio(int mes, int ano, Predio? predio = null)
    {
        var alvo = predio ?? _predio;
        var relatorio = new Relatorio(mes, ano, $"Relatorio_{ano}-{mes:D2}.pdf", $"relatorios/{alvo.Id}/{Guid.NewGuid():N}.pdf", alvo.Id);
        await _sut.AdicionarAsync(relatorio);
        return relatorio;
    }

    private Compromisso AdicionarCompromisso(
        string titulo, DateOnly data, TimeOnly horario, bool concluido, Predio? predio = null)
    {
        var compromisso = new Compromisso(titulo, data, horario, (predio ?? _predio).Id, _sindico.Id);
        if (concluido)
        {
            compromisso.Concluir();
        }

        _context.Compromissos.Add(compromisso);
        _context.SaveChanges();
        return compromisso;
    }
}
