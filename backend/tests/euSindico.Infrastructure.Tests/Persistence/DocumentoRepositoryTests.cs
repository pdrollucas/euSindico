using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using euSindico.Infrastructure.Persistence;
using euSindico.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Tests.Persistence;

/// <summary>Roda <see cref="DocumentoRepository"/> contra SQLite em memória.</summary>
public class DocumentoRepositoryTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly DocumentoRepository _sut;

    private readonly Usuario _sindico = new("Sindico", "s@x.com", "hash");
    private readonly Predio _predio;
    private readonly Predio _outroPredio;
    private readonly Predio _predioExcluido;

    public DocumentoRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
        _sut = new DocumentoRepository(_context);

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

    [Fact]
    public async Task Tipos_de_documento_semeados_batem_com_o_enum()
    {
        var tipos = await _context.TiposDocumento.AsNoTracking().ToDictionaryAsync(t => t.Id, t => t.Nome);

        Assert.Equal("Atas", tipos[(int)TipoDocumentoPredio.Ata]);
        Assert.Equal("Normas", tipos[(int)TipoDocumentoPredio.Norma]);
    }

    [Fact]
    public async Task Lista_do_enviado_mais_recentemente_para_o_mais_antigo()
    {
        var primeiro = await Adicionar("a.pdf");
        var segundo = await Adicionar("b.pdf");
        var terceiro = await Adicionar("c.pdf");

        var (itens, total) = await _sut.ListarAsync(_predio.Id, TipoDocumentoPredio.Ata, null, 1, 10);

        Assert.Equal(3, total);
        Assert.Equal(new[] { terceiro.Id, segundo.Id, primeiro.Id }, itens.Select(d => d.Id));
    }

    [Fact]
    public async Task Lista_so_o_tipo_pedido()
    {
        var ata = await Adicionar("ata.pdf", TipoDocumentoPredio.Ata);
        var norma = await Adicionar("norma.pdf", TipoDocumentoPredio.Norma);

        var (atas, totalAtas) = await _sut.ListarAsync(_predio.Id, TipoDocumentoPredio.Ata, null, 1, 10);
        var (normas, totalNormas) = await _sut.ListarAsync(_predio.Id, TipoDocumentoPredio.Norma, null, 1, 10);

        Assert.Equal(1, totalAtas);
        Assert.Equal(ata.Id, Assert.Single(atas).Id);
        Assert.Equal(1, totalNormas);
        Assert.Equal(norma.Id, Assert.Single(normas).Id);
    }

    [Fact]
    public async Task Lista_so_os_documentos_do_predio_pedido()
    {
        var doPredio = await Adicionar("a.pdf");
        await Adicionar("b.pdf", predio: _outroPredio);

        var (itens, total) = await _sut.ListarAsync(_predio.Id, TipoDocumentoPredio.Ata, null, 1, 10);

        Assert.Equal(1, total);
        Assert.Equal(doPredio.Id, Assert.Single(itens).Id);
    }

    [Fact]
    public async Task Busca_por_trecho_do_nome()
    {
        await Adicionar("Ata assembleia 2026.pdf");
        var alvo = await Adicionar("Convocação extraordinária.pdf");
        await Adicionar("Outra coisa.pdf");

        var (itens, total) = await _sut.ListarAsync(_predio.Id, TipoDocumentoPredio.Ata, "extraord", 1, 10);

        Assert.Equal(1, total);
        Assert.Equal(alvo.Id, Assert.Single(itens).Id);
    }

    [Fact]
    public async Task Paginacao_informa_o_total_e_respeita_a_ordem()
    {
        var a = await Adicionar("a.pdf");
        var b = await Adicionar("b.pdf");
        var c = await Adicionar("c.pdf");

        var (primeira, total) = await _sut.ListarAsync(_predio.Id, TipoDocumentoPredio.Ata, null, 1, 2);
        var (segunda, _) = await _sut.ListarAsync(_predio.Id, TipoDocumentoPredio.Ata, null, 2, 2);

        Assert.Equal(3, total);
        Assert.Equal(new[] { c.Id, b.Id }, primeira.Select(d => d.Id));
        Assert.Equal(new[] { a.Id }, segunda.Select(d => d.Id));
    }

    [Fact]
    public async Task Predio_excluido_nao_devolve_documentos_RN08()
    {
        await Adicionar("a.pdf", predio: _predioExcluido);

        var (itens, total) = await _sut.ListarAsync(_predioExcluido.Id, TipoDocumentoPredio.Ata, null, 1, 10);

        Assert.Empty(itens);
        Assert.Equal(0, total);
    }

    [Fact]
    public async Task BuscarPorId_carrega_o_predio()
    {
        var documento = await Adicionar("a.pdf");
        _context.ChangeTracker.Clear();

        var encontrado = await _sut.BuscarPorIdAsync(documento.Id);

        Assert.Equal("Predio A", encontrado!.Predio!.Nome);
    }

    [Fact]
    public async Task BuscarPorId_inexistente_devolve_null()
    {
        Assert.Null(await _sut.BuscarPorIdAsync(999));
    }

    [Fact]
    public async Task Contar_considera_so_predio_e_tipo_pedidos()
    {
        await Adicionar("a.pdf", TipoDocumentoPredio.Norma);
        await Adicionar("b.pdf", TipoDocumentoPredio.Norma);
        await Adicionar("c.pdf", TipoDocumentoPredio.Ata);
        await Adicionar("d.pdf", TipoDocumentoPredio.Norma, _outroPredio);

        Assert.Equal(2, await _sut.ContarAsync(_predio.Id, TipoDocumentoPredio.Norma));
        Assert.Equal(1, await _sut.ContarAsync(_predio.Id, TipoDocumentoPredio.Ata));
    }

    [Fact]
    public async Task Contar_com_intervalo_inclui_o_inicio_e_exclui_o_fim()
    {
        var inicio = new DateTime(2026, 10, 1, 3, 0, 0, DateTimeKind.Utc);
        var fim = new DateTime(2026, 11, 1, 3, 0, 0, DateTimeKind.Utc);

        await AdicionarEm("antes.pdf", inicio.AddTicks(-1));
        await AdicionarEm("no-inicio.pdf", inicio);
        await AdicionarEm("no-meio.pdf", inicio.AddDays(10));
        await AdicionarEm("no-fim.pdf", fim);

        Assert.Equal(2, await _sut.ContarAsync(_predio.Id, TipoDocumentoPredio.Ata, inicio, fim));
        Assert.Equal(4, await _sut.ContarAsync(_predio.Id, TipoDocumentoPredio.Ata));
    }

    [Fact]
    public async Task Contar_libera_vaga_depois_de_remover()
    {
        var documento = await Adicionar("a.pdf", TipoDocumentoPredio.Norma);

        await _sut.RemoverAsync(documento);

        Assert.Equal(0, await _sut.ContarAsync(_predio.Id, TipoDocumentoPredio.Norma));
    }

    [Fact]
    public async Task Remover_apaga_o_registro()
    {
        var documento = await Adicionar("a.pdf");

        await _sut.RemoverAsync(documento);

        Assert.Null(await _sut.BuscarPorIdAsync(documento.Id));
    }

    // Grava com data de criação escolhida (a entidade usa DateTime.UtcNow e não expõe setter).
    private async Task AdicionarEm(string nome, DateTime criadoEmUtc)
    {
        var documento = new Documento(nome, $"documentos/{_predio.Id}/{Guid.NewGuid():N}.pdf", (int)TipoDocumentoPredio.Ata, _predio.Id);
        typeof(Documento).GetProperty(nameof(Documento.CriadoEm))!.SetValue(documento, criadoEmUtc);
        await _sut.AdicionarAsync(documento);
    }

    private async Task<Documento> Adicionar(
        string nome, TipoDocumentoPredio tipo = TipoDocumentoPredio.Ata, Predio? predio = null)
    {
        var alvo = predio ?? _predio;
        var documento = new Documento(nome, $"documentos/{alvo.Id}/{Guid.NewGuid():N}.pdf", (int)tipo, alvo.Id);
        await _sut.AdicionarAsync(documento);
        return documento;
    }
}
