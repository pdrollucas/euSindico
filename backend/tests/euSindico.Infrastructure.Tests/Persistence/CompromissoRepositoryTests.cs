using euSindico.Application.Compromissos;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;
using euSindico.Infrastructure.Persistence;
using euSindico.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace euSindico.Infrastructure.Tests.Persistence;

/// <summary>
/// Roda o <see cref="CompromissoRepository"/> contra SQLite em memória — valida a lógica de
/// visibilidade (papel por prédio, escopo, situação) e a ordem de exclusão de conta com FKs ativas.
/// Não substitui um teste contra MySQL (a tradução de <c>Contains</c>, por exemplo, difere em
/// maiúsculas/minúsculas entre os provedores), mas cobre as regras de negócio da consulta.
/// </summary>
public class CompromissoRepositoryTests : IDisposable
{
    private static readonly DateOnly Hoje = new(2026, 10, 17);
    private static readonly TimeOnly Agora = new(15, 0);

    private readonly SqliteConnection _connection;
    private readonly AppDbContext _context;
    private readonly CompromissoRepository _sut;

    private readonly Usuario _sindicoA = new("Sindico A", "a@x.com", "hash");
    private readonly Usuario _gestor = new("Gestor", "g@x.com", "hash");
    private readonly Usuario _colaborador = new("Colaborador", "c@x.com", "hash");
    private readonly Usuario _sindicoB = new("Sindico B", "b@x.com", "hash");
    private readonly Usuario _semVinculo = new("Sem vinculo", "s@x.com", "hash");

    private readonly Predio _predioA;
    private readonly Predio _predioB;
    private readonly Predio _predioExcluido;

    public CompromissoRepositoryTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();
        _context = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite(_connection).Options);
        _context.Database.EnsureCreated();
        _sut = new CompromissoRepository(_context);

        _context.Usuarios.AddRange(_sindicoA, _gestor, _colaborador, _sindicoB, _semVinculo);
        _context.SaveChanges();

        _predioA = new Predio("Predio A", "Rua A, 1", _sindicoA.Id);
        _predioB = new Predio("Predio B", "Rua B, 1", _sindicoB.Id);
        _predioExcluido = new Predio("Predio Excluido", "Rua C, 1", _sindicoA.Id);
        _predioExcluido.ExcluirLogicamente();
        _context.Predios.AddRange(_predioA, _predioB, _predioExcluido);
        _context.SaveChanges();

        _context.PredioUsuarios.AddRange(
            PredioUsuario.CriarComoDono(_predioA.Id, _sindicoA.Id),
            PredioUsuario.CriarComoConvidado(_predioA.Id, _gestor.Id, PapelPredio.Gestor, _sindicoA.Id),
            PredioUsuario.CriarComoConvidado(_predioA.Id, _colaborador.Id, PapelPredio.Colaborador, _sindicoA.Id),
            PredioUsuario.CriarComoDono(_predioB.Id, _sindicoB.Id),
            // O mesmo usuário é Síndico em A e apenas Colaborador em B (RN18).
            PredioUsuario.CriarComoConvidado(_predioB.Id, _sindicoA.Id, PapelPredio.Colaborador, _sindicoB.Id),
            PredioUsuario.CriarComoDono(_predioExcluido.Id, _sindicoA.Id));
        _context.SaveChanges();
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }

    // ---------- Escopo, papel e vínculo ----------

    [Fact]
    public async Task Escopo_meus_devolve_so_os_compromissos_do_usuario_em_todos_os_predios()
    {
        var meuEmA = Adicionar("Meu em A", 18, _predioA, _sindicoA);
        var meuEmB = Adicionar("Meu em B", 18, _predioB, _sindicoA);
        Adicionar("Do gestor em A", 18, _predioA, _gestor);
        Adicionar("Do sindico B em B", 18, _predioB, _sindicoB);

        var (itens, total) = await _sut.ListarAsync(Filtro(_sindicoA, EscopoCompromissos.Meus), 1, 10);

        Assert.Equal(2, total);
        Assert.Equivalent(new[] { meuEmA.Id, meuEmB.Id }, itens.Select(c => c.Id));
    }

    [Fact]
    public async Task Escopo_todos_como_Sindico_devolve_tudo_do_predio_onde_e_Sindico_e_so_os_proprios_onde_e_Colaborador()
    {
        var meuEmA = Adicionar("Meu em A", 18, _predioA, _sindicoA);
        var doGestorEmA = Adicionar("Do gestor em A", 18, _predioA, _gestor);
        var doColabEmA = Adicionar("Do colaborador em A", 18, _predioA, _colaborador);
        var meuEmB = Adicionar("Meu em B", 18, _predioB, _sindicoA);
        Adicionar("Do sindico B em B", 18, _predioB, _sindicoB);

        var (itens, _) = await _sut.ListarAsync(Filtro(_sindicoA, EscopoCompromissos.Todos), 1, 10);

        Assert.Equivalent(
            new[] { meuEmA.Id, doGestorEmA.Id, doColabEmA.Id, meuEmB.Id },
            itens.Select(c => c.Id));
    }

    [Fact]
    public async Task Escopo_todos_como_Gestor_devolve_todos_do_predio()
    {
        var doSindico = Adicionar("Do sindico", 18, _predioA, _sindicoA);
        var doColab = Adicionar("Do colaborador", 18, _predioA, _colaborador);

        var (itens, _) = await _sut.ListarAsync(Filtro(_gestor, EscopoCompromissos.Todos), 1, 10);

        Assert.Equivalent(new[] { doSindico.Id, doColab.Id }, itens.Select(c => c.Id));
    }

    [Fact]
    public async Task Escopo_todos_como_Colaborador_continua_devolvendo_so_os_proprios_RN21()
    {
        Adicionar("Do sindico", 18, _predioA, _sindicoA);
        Adicionar("Do gestor", 18, _predioA, _gestor);
        var meu = Adicionar("Do colaborador", 18, _predioA, _colaborador);

        var (itens, total) = await _sut.ListarAsync(Filtro(_colaborador, EscopoCompromissos.Todos), 1, 10);

        Assert.Equal(1, total);
        Assert.Equal(meu.Id, Assert.Single(itens).Id);
    }

    [Fact]
    public async Task Usuario_sem_vinculo_com_o_predio_nao_ve_nada_mesmo_no_escopo_todos()
    {
        Adicionar("Do sindico", 18, _predioA, _sindicoA);

        var (itens, total) = await _sut.ListarAsync(Filtro(_semVinculo, EscopoCompromissos.Todos), 1, 10);

        Assert.Empty(itens);
        Assert.Equal(0, total);
    }

    [Fact]
    public async Task Compromissos_de_predio_excluido_nao_aparecem_RN08()
    {
        Adicionar("Em predio excluido", 18, _predioExcluido, _sindicoA);
        var visivel = Adicionar("Em predio ativo", 18, _predioA, _sindicoA);

        var (itens, _) = await _sut.ListarAsync(Filtro(_sindicoA, EscopoCompromissos.Todos), 1, 10);

        Assert.Equal(visivel.Id, Assert.Single(itens).Id);
    }

    [Fact]
    public async Task Filtro_por_predio_restringe_a_um_predio()
    {
        Adicionar("Em A", 18, _predioA, _sindicoA);
        var emB = Adicionar("Em B", 18, _predioB, _sindicoA);

        var (itens, _) = await _sut.ListarAsync(Filtro(_sindicoA, EscopoCompromissos.Meus, predioId: _predioB.Id), 1, 10);

        Assert.Equal(emB.Id, Assert.Single(itens).Id);
    }

    [Fact]
    public async Task Filtro_por_titulo_busca_por_trecho()
    {
        var alvo = Adicionar("Reunião de condomínio", 18, _predioA, _sindicoA);
        Adicionar("Visita técnica", 18, _predioA, _sindicoA);

        var (itens, _) = await _sut.ListarAsync(Filtro(_sindicoA, titulo: "condomínio"), 1, 10);

        Assert.Equal(alvo.Id, Assert.Single(itens).Id);
    }

    // ---------- Mês e ano ----------

    [Fact]
    public async Task Filtro_por_ano_e_mes_devolve_so_os_compromissos_daquele_mes_incluindo_as_bordas()
    {
        var primeiroDia = AdicionarEm("Primeiro dia", new DateOnly(2026, 11, 1));
        var ultimoDia = AdicionarEm("Ultimo dia", new DateOnly(2026, 11, 30));
        AdicionarEm("Mes anterior", new DateOnly(2026, 10, 31));
        AdicionarEm("Mes seguinte", new DateOnly(2026, 12, 1));
        AdicionarEm("Mesmo mes, outro ano", new DateOnly(2027, 11, 15));

        var (itens, total) = await _sut.ListarAsync(Filtro(_sindicoA, ano: 2026, mes: 11), 1, 10);

        Assert.Equal(new[] { primeiroDia.Id, ultimoDia.Id }, itens.Select(c => c.Id));
        Assert.Equal(2, total);
    }

    [Fact]
    public async Task Filtro_por_mes_combina_com_a_situacao()
    {
        var pendente = AdicionarEm("Pendente", new DateOnly(2026, 11, 5));
        var concluido = AdicionarEm("Concluido", new DateOnly(2026, 11, 6), concluido: true);

        var (proximos, _) = await _sut.ListarAsync(Filtro(_sindicoA, ano: 2026, mes: 11), 1, 10);
        var (concluidos, _) = await _sut.ListarAsync(
            Filtro(_sindicoA, situacao: SituacaoCompromissos.Concluidos, ano: 2026, mes: 11), 1, 10);

        Assert.Equal(pendente.Id, Assert.Single(proximos).Id);
        Assert.Equal(concluido.Id, Assert.Single(concluidos).Id);
    }

    [Fact]
    public async Task Situacao_todos_devolve_concluidos_e_pendentes_de_qualquer_data_e_respeita_o_periodo()
    {
        var pendente = AdicionarEm("Pendente", new DateOnly(2026, 9, 5));
        var concluido = AdicionarEm("Concluido", new DateOnly(2026, 9, 20), concluido: true);
        var outroMes = AdicionarEm("Outro mes", new DateOnly(2026, 11, 2));

        var (semPeriodo, _) = await _sut.ListarAsync(Filtro(_sindicoA, situacao: SituacaoCompromissos.Todos), 1, 10);
        var (setembro, _) = await _sut.ListarAsync(
            Filtro(_sindicoA, situacao: SituacaoCompromissos.Todos, ano: 2026, mes: 9), 1, 10);

        Assert.Equal(new[] { pendente.Id, concluido.Id, outroMes.Id }, semPeriodo.Select(c => c.Id));
        Assert.Equal(new[] { pendente.Id, concluido.Id }, setembro.Select(c => c.Id));
    }

    // ---------- Situação (hoje = 17/10/2026, agora = 15:00) ----------

    [Fact]
    public async Task Situacao_proximos_devolve_nao_concluidos_de_hoje_em_diante()
    {
        var ontem = Adicionar("Ontem pendente", 16, _predioA, _sindicoA, hora: new TimeOnly(10, 0));
        var hojeCedo = Adicionar("Hoje cedo", 17, _predioA, _sindicoA, hora: new TimeOnly(10, 0));
        var hojeTarde = Adicionar("Hoje tarde", 17, _predioA, _sindicoA, hora: new TimeOnly(18, 0));
        var amanha = Adicionar("Amanhã", 18, _predioA, _sindicoA);
        Adicionar("Hoje concluído", 17, _predioA, _sindicoA, concluido: true);

        var (itens, _) = await _sut.ListarAsync(Filtro(_sindicoA, situacao: SituacaoCompromissos.Proximos), 1, 10);

        Assert.Equal(new[] { hojeCedo.Id, hojeTarde.Id, amanha.Id }, itens.Select(c => c.Id));
        Assert.DoesNotContain(itens, c => c.Id == ontem.Id);
    }

    [Fact]
    public async Task Situacao_hoje_devolve_so_nao_concluidos_com_data_de_hoje()
    {
        Adicionar("Ontem", 16, _predioA, _sindicoA);
        var hojeCedo = Adicionar("Hoje cedo", 17, _predioA, _sindicoA, hora: new TimeOnly(10, 0));
        var hojeTarde = Adicionar("Hoje tarde", 17, _predioA, _sindicoA, hora: new TimeOnly(18, 0));
        Adicionar("Amanhã", 18, _predioA, _sindicoA);
        Adicionar("Hoje concluído", 17, _predioA, _sindicoA, concluido: true);

        var (itens, _) = await _sut.ListarAsync(Filtro(_sindicoA, situacao: SituacaoCompromissos.Hoje), 1, 10);

        Assert.Equal(new[] { hojeCedo.Id, hojeTarde.Id }, itens.Select(c => c.Id));
    }

    [Fact]
    public async Task Situacao_atrasados_considera_a_data_e_o_horario_de_hoje()
    {
        var ontem = Adicionar("Ontem pendente", 16, _predioA, _sindicoA, hora: new TimeOnly(23, 0));
        var hojeCedo = Adicionar("Hoje cedo", 17, _predioA, _sindicoA, hora: new TimeOnly(10, 0));
        Adicionar("Hoje tarde", 17, _predioA, _sindicoA, hora: new TimeOnly(18, 0));
        Adicionar("Amanhã", 18, _predioA, _sindicoA);
        Adicionar("Ontem concluído", 16, _predioA, _sindicoA, concluido: true);

        var (itens, _) = await _sut.ListarAsync(Filtro(_sindicoA, situacao: SituacaoCompromissos.Atrasados), 1, 10);

        Assert.Equal(new[] { ontem.Id, hojeCedo.Id }, itens.Select(c => c.Id));
    }

    [Fact]
    public async Task Compromisso_exatamente_no_horario_atual_ainda_nao_e_atrasado()
    {
        Adicionar("Agora", 17, _predioA, _sindicoA, hora: Agora);

        var (itens, _) = await _sut.ListarAsync(Filtro(_sindicoA, situacao: SituacaoCompromissos.Atrasados), 1, 10);

        Assert.Empty(itens);
    }

    [Fact]
    public async Task Situacao_concluidos_devolve_todos_os_concluidos_em_qualquer_data()
    {
        var antigo = Adicionar("Antigo", 1, _predioA, _sindicoA, concluido: true);
        var futuro = Adicionar("Futuro", 25, _predioA, _sindicoA, concluido: true);
        Adicionar("Pendente", 18, _predioA, _sindicoA);

        var (itens, _) = await _sut.ListarAsync(Filtro(_sindicoA, situacao: SituacaoCompromissos.Concluidos), 1, 10);

        Assert.Equal(new[] { antigo.Id, futuro.Id }, itens.Select(c => c.Id));
    }

    // ---------- Ordenação e paginação (RN14, RNF10) ----------

    [Fact]
    public async Task Ordena_por_data_depois_horario_e_pagina_com_total()
    {
        var c3 = Adicionar("Terceiro", 19, _predioA, _sindicoA, hora: new TimeOnly(8, 0));
        var c1 = Adicionar("Primeiro", 18, _predioA, _sindicoA, hora: new TimeOnly(9, 0));
        var c2 = Adicionar("Segundo", 18, _predioA, _sindicoA, hora: new TimeOnly(14, 0));

        var (pagina1, total) = await _sut.ListarAsync(Filtro(_sindicoA), 1, 2);
        var (pagina2, _) = await _sut.ListarAsync(Filtro(_sindicoA), 2, 2);

        Assert.Equal(3, total);
        Assert.Equal(new[] { c1.Id, c2.Id }, pagina1.Select(c => c.Id));
        Assert.Equal(new[] { c3.Id }, pagina2.Select(c => c.Id));
    }

    [Fact]
    public async Task Listagem_carrega_predio_e_responsavel()
    {
        Adicionar("Visita", 18, _predioA, _gestor);

        var (itens, _) = await _sut.ListarAsync(Filtro(_sindicoA, EscopoCompromissos.Todos), 1, 10);

        var item = Assert.Single(itens);
        Assert.Equal("Predio A", item.Predio!.Nome);
        Assert.Equal("Gestor", item.Responsavel!.Nome);
    }

    // ---------- Busca, adição, atualização e remoção ----------

    [Fact]
    public async Task BuscarPorIdAsync_carrega_predio_e_responsavel_e_devolve_null_se_nao_existe()
    {
        var compromisso = Adicionar("Visita", 18, _predioA, _gestor);
        _context.ChangeTracker.Clear();

        var encontrado = await _sut.BuscarPorIdAsync(compromisso.Id);

        Assert.NotNull(encontrado);
        Assert.Equal("Predio A", encontrado.Predio!.Nome);
        Assert.Equal("Gestor", encontrado.Responsavel!.Nome);
        Assert.Null(await _sut.BuscarPorIdAsync(9999));
    }

    [Fact]
    public async Task AtualizarAsync_persiste_a_mudanca_de_predio_e_de_responsavel()
    {
        var compromisso = Adicionar("Visita", 18, _predioA, _gestor);
        var id = compromisso.Id;
        _context.ChangeTracker.Clear();

        var carregado = (await _sut.BuscarPorIdAsync(id))!;
        carregado.AtualizarDados("Visita", Hoje, Agora, null, null, _predioB.Id, _sindicoA.Id, 1234.56m);
        await _sut.AtualizarAsync(carregado);
        _context.ChangeTracker.Clear();

        var recarregado = (await _sut.BuscarPorIdAsync(id))!;
        Assert.Equal(1234.56m, recarregado.Orcamento);
        Assert.Equal(_predioB.Id, recarregado.PredioId);
        Assert.Equal(_sindicoA.Id, recarregado.ResponsavelUsuarioId);
        Assert.Equal("Predio B", recarregado.Predio!.Nome);
    }

    [Fact]
    public async Task RemoverAsync_apaga_a_linha()
    {
        var compromisso = Adicionar("Visita", 18, _predioA, _gestor);
        var id = compromisso.Id;

        await _sut.RemoverAsync(compromisso);
        _context.ChangeTracker.Clear();

        Assert.Null(await _sut.BuscarPorIdAsync(id));
    }

    // ---------- Exclusão de conta (RF07) com as FKs ativas ----------

    [Fact]
    public async Task Excluir_conta_do_Sindico_apaga_prédios_equipe_convites_e_compromissos_sem_violar_FK()
    {
        Adicionar("Do sindico", 18, _predioA, _sindicoA);
        Adicionar("Do gestor em A", 18, _predioA, _gestor);
        Adicionar("Do colab em A", 18, _predioA, _colaborador);
        _context.ConvitesFuncionario.Add(new ConviteFuncionario(_predioA.Id, "novo@x.com", PapelPredio.Gestor, "hash-do-token", _sindicoA.Id, DateTime.UtcNow.AddDays(7)));
        _context.SaveChanges();
        var sindicoId = _sindicoA.Id;
        var predioAId = _predioA.Id;

        var prediosApagados = await new UsuarioRepository(_context).ExcluirUsuarioEDadosRelacionadosAsync(sindicoId);
        _context.ChangeTracker.Clear();

        // Devolve todos os prédios que eram dele — inclusive o de exclusão lógica, cujos arquivos também
        // precisam sair do armazenamento — e nenhum de outro síndico.
        Assert.Equal(
            new[] { predioAId, _predioExcluido.Id }.OrderBy(id => id),
            prediosApagados.OrderBy(id => id));
        Assert.DoesNotContain(_predioB.Id, prediosApagados);

        Assert.False(await _context.Usuarios.AnyAsync(u => u.Id == sindicoId));
        Assert.False(await _context.Predios.AnyAsync(p => p.Id == predioAId));
        Assert.False(await _context.Compromissos.AnyAsync(c => c.PredioId == predioAId));
        Assert.False(await _context.PredioUsuarios.AnyAsync(pu => pu.PredioId == predioAId));
        Assert.False(await _context.PredioUsuarios.AnyAsync(pu => pu.UsuarioId == sindicoId));
        Assert.False(await _context.ConvitesFuncionario.AnyAsync(c => c.PredioId == predioAId));

        // A equipe continua com a conta; só perde o acesso ao prédio que deixou de existir.
        Assert.True(await _context.Usuarios.AnyAsync(u => u.Id == _gestor.Id));
        Assert.True(await _context.Usuarios.AnyAsync(u => u.Id == _colaborador.Id));
        Assert.True(await _context.Predios.AnyAsync(p => p.Id == _predioB.Id));
    }

    [Fact]
    public async Task Excluir_conta_de_funcionario_reatribui_seus_compromissos_ao_dono_do_predio_e_remove_o_vinculo()
    {
        var doColab = Adicionar("Do colaborador", 18, _predioA, _colaborador);
        var concluidoDoColab = Adicionar("Concluído do colaborador", 10, _predioA, _colaborador, concluido: true);
        var doGestor = Adicionar("Do gestor", 18, _predioA, _gestor);
        var colabId = _colaborador.Id;

        var prediosApagados = await new UsuarioRepository(_context).ExcluirUsuarioEDadosRelacionadosAsync(colabId);
        _context.ChangeTracker.Clear();

        // Funcionário não é dono de prédio nenhum: nada para limpar no armazenamento.
        Assert.Empty(prediosApagados);

        Assert.False(await _context.Usuarios.AnyAsync(u => u.Id == colabId));
        Assert.False(await _context.PredioUsuarios.AnyAsync(pu => pu.UsuarioId == colabId));

        var reatribuido = await _context.Compromissos.SingleAsync(c => c.Id == doColab.Id);
        Assert.Equal(_sindicoA.Id, reatribuido.ResponsavelUsuarioId);
        var concluido = await _context.Compromissos.SingleAsync(c => c.Id == concluidoDoColab.Id);
        Assert.Equal(_sindicoA.Id, concluido.ResponsavelUsuarioId);
        Assert.True(concluido.Concluido);

        var intocado = await _context.Compromissos.SingleAsync(c => c.Id == doGestor.Id);
        Assert.Equal(_gestor.Id, intocado.ResponsavelUsuarioId);
        Assert.True(await _context.Predios.AnyAsync(p => p.Id == _predioA.Id));
    }

    [Fact]
    public async Task Excluir_conta_de_usuario_sem_dados_nao_falha()
    {
        var id = _semVinculo.Id;

        await new UsuarioRepository(_context).ExcluirUsuarioEDadosRelacionadosAsync(id);
        _context.ChangeTracker.Clear();

        Assert.False(await _context.Usuarios.AnyAsync(u => u.Id == id));
    }

    // ---------- Helpers ----------

    private Compromisso Adicionar(
        string titulo, int dia, Predio predio, Usuario responsavel, TimeOnly? hora = null, bool concluido = false) =>
        Adicionar(titulo, new DateOnly(2026, 10, dia), predio, responsavel, hora, concluido);

    // Do _sindicoA no _predioA, para os testes de mês/ano que precisam de datas fora de outubro/2026.
    private Compromisso AdicionarEm(string titulo, DateOnly data, bool concluido = false) =>
        Adicionar(titulo, data, _predioA, _sindicoA, null, concluido);

    private Compromisso Adicionar(
        string titulo, DateOnly data, Predio predio, Usuario responsavel, TimeOnly? hora, bool concluido)
    {
        var compromisso = new Compromisso(titulo, data, hora ?? new TimeOnly(9, 0), predio.Id, responsavel.Id);
        if (concluido)
        {
            compromisso.Concluir();
        }

        _context.Compromissos.Add(compromisso);
        _context.SaveChanges();
        return compromisso;
    }

    private static CompromissoFiltro Filtro(
        Usuario usuario,
        EscopoCompromissos escopo = EscopoCompromissos.Meus,
        SituacaoCompromissos situacao = SituacaoCompromissos.Proximos,
        int? predioId = null,
        string? titulo = null,
        int? ano = null,
        int? mes = null) =>
        new(usuario.Id, titulo, predioId, escopo, situacao, Hoje, Agora, ano, mes);
}
