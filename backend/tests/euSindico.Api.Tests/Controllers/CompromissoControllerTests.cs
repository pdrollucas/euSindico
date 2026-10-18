using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Api.Controllers;
using euSindico.Api.Validators;
using euSindico.Application.Common.Dtos;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Compromissos;
using euSindico.Application.Compromissos.Dtos;
using euSindico.Application.Equipe;
using euSindico.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace euSindico.Api.Tests.Controllers;

/// <summary>
/// Testa o <see cref="CompromissoController"/> com um <see cref="CompromissoService"/> real e os
/// repositórios mockados (mesmo padrão de PredioControllerTests). O id do usuário vem da claim "sub".
/// </summary>
public class CompromissoControllerTests
{
    private const int UsuarioId = 1;
    private const int PredioId = 10;

    private readonly Mock<ICompromissoRepository> _compromissoRepository = new();
    private readonly Mock<IPredioUsuarioRepository> _predioUsuarioRepository = new();
    private readonly CompromissoController _sut;

    public CompromissoControllerTests()
    {
        var autorizacao = new AutorizacaoPredioService(_predioUsuarioRepository.Object);
        var service = new CompromissoService(_compromissoRepository.Object, _predioUsuarioRepository.Object, autorizacao, TimeProvider.System);

        var vinculo = PredioUsuario.CriarComoDono(PredioId, UsuarioId);
        Definir(vinculo, nameof(PredioUsuario.Predio), new Predio("Edifício Aurora", "Rua X, 100", UsuarioId));
        Definir(vinculo, nameof(PredioUsuario.Usuario), new Usuario("Pedro", "pedro@x.com", "hash"));
        _predioUsuarioRepository
            .Setup(r => r.BuscarVinculoAsync(PredioId, UsuarioId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vinculo);

        var claims = new[] { new Claim(JwtRegisteredClaimNames.Sub, UsuarioId.ToString()) };
        _sut = new CompromissoController(service, new CompromissoFormDtoValidator(), new AlterarConclusaoDtoValidator())
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(new ClaimsIdentity(claims)) },
            },
        };
    }

    private static CompromissoFormDto FormValido() =>
        new(PredioId, "Visita técnica", new DateOnly(2026, 10, 17), new TimeOnly(14, 30), null, null, null);

    [Fact]
    public async Task Criar_com_dados_validos_retorna_201_com_location_e_corpo()
    {
        var resultado = await _sut.Criar(FormValido(), CancellationToken.None);

        var criado = Assert.IsType<CreatedAtActionResult>(resultado);
        Assert.Equal(StatusCodes.Status201Created, criado.StatusCode);
        Assert.Equal(nameof(CompromissoController.Obter), criado.ActionName);
        var corpo = Assert.IsType<CompromissoDto>(criado.Value);
        Assert.Equal("Visita técnica", corpo.Titulo);
        Assert.Equal(UsuarioId, corpo.ResponsavelUsuarioId);
    }

    [Fact]
    public async Task Criar_com_titulo_invalido_retorna_400_e_nao_persiste()
    {
        var resultado = await _sut.Criar(FormValido() with { Titulo = "<script>alert(1)</script>" }, CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
        _compromissoRepository.Verify(r => r.AdicionarAsync(It.IsAny<Compromisso>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Criar_sem_horario_retorna_400()
    {
        var resultado = await _sut.Criar(FormValido() with { HorarioCompromisso = null }, CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task Listar_com_defaults_chama_o_service_com_escopo_meus_e_situacao_proximos()
    {
        CompromissoFiltro? capturado = null;
        _compromissoRepository
            .Setup(r => r.ListarAsync(It.IsAny<CompromissoFiltro>(), 1, 10, It.IsAny<CancellationToken>()))
            .Callback((CompromissoFiltro f, int _, int _, CancellationToken _) => capturado = f)
            .ReturnsAsync((new List<Compromisso>(), 0));

        var resultado = await _sut.Listar();

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.IsType<PagedResultDto<CompromissoDto>>(ok.Value);
        Assert.Equal(EscopoCompromissos.Meus, capturado!.Escopo);
        Assert.Equal(SituacaoCompromissos.Proximos, capturado.Situacao);
        Assert.Equal(UsuarioId, capturado.UsuarioId);
    }

    [Theory]
    [InlineData(0, 10)]
    [InlineData(-1, 10)]
    [InlineData(1, 0)]
    [InlineData(1, 21)]
    public async Task Listar_com_paginacao_fora_da_faixa_retorna_400(int page, int pageSize)
    {
        var resultado = await _sut.Listar(page: page, pageSize: pageSize);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task Listar_com_page_size_20_e_aceito()
    {
        _compromissoRepository
            .Setup(r => r.ListarAsync(It.IsAny<CompromissoFiltro>(), 1, 20, It.IsAny<CancellationToken>()))
            .ReturnsAsync((new List<Compromisso>(), 0));

        var resultado = await _sut.Listar(pageSize: 20);

        Assert.IsType<OkObjectResult>(resultado);
    }

    [Fact]
    public async Task Listar_com_enum_numerico_fora_do_intervalo_retorna_400()
    {
        var escopo = await _sut.Listar(escopo: (EscopoCompromissos)9);
        var situacao = await _sut.Listar(situacao: (SituacaoCompromissos)9);

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<ObjectResult>(escopo).StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<ObjectResult>(situacao).StatusCode);
    }

    [Fact]
    public async Task Listar_com_titulo_muito_longo_ou_predio_invalido_retorna_400()
    {
        var titulo = await _sut.Listar(titulo: new string('a', 151));
        var predio = await _sut.Listar(predioId: 0);

        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<ObjectResult>(titulo).StatusCode);
        Assert.Equal(StatusCodes.Status400BadRequest, Assert.IsAssignableFrom<ObjectResult>(predio).StatusCode);
    }

    [Fact]
    public async Task AlterarConclusao_sem_valor_retorna_400_e_nao_consulta()
    {
        var resultado = await _sut.AlterarConclusao(100, new AlterarConclusaoDto(null), CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
        _compromissoRepository.Verify(r => r.BuscarPorIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task AlterarConclusao_com_valor_retorna_200_com_o_estado_novo()
    {
        ConfigurarCompromisso();

        var resultado = await _sut.AlterarConclusao(100, new AlterarConclusaoDto(true), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.True(Assert.IsType<CompromissoDto>(ok.Value).Concluido);
    }

    [Fact]
    public async Task Obter_retorna_200_com_corpo()
    {
        ConfigurarCompromisso();

        var resultado = await _sut.Obter(100, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal(100, Assert.IsType<CompromissoDto>(ok.Value).Id);
    }

    [Fact]
    public async Task Atualizar_com_dados_invalidos_retorna_400()
    {
        var resultado = await _sut.Atualizar(100, FormValido() with { DataCompromisso = null }, CancellationToken.None);

        var objeto = Assert.IsAssignableFrom<ObjectResult>(resultado);
        Assert.Equal(StatusCodes.Status400BadRequest, objeto.StatusCode);
    }

    [Fact]
    public async Task Atualizar_com_dados_validos_retorna_200()
    {
        ConfigurarCompromisso();

        var resultado = await _sut.Atualizar(100, FormValido() with { Titulo = "Novo" }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(resultado);
        Assert.Equal("Novo", Assert.IsType<CompromissoDto>(ok.Value).Titulo);
    }

    [Fact]
    public async Task Remover_retorna_204()
    {
        ConfigurarCompromisso();

        var resultado = await _sut.Remover(100, CancellationToken.None);

        Assert.IsType<NoContentResult>(resultado);
    }

    private void ConfigurarCompromisso()
    {
        var compromisso = new Compromisso("Visita técnica", new DateOnly(2026, 10, 17), new TimeOnly(14, 30), PredioId, UsuarioId);
        Definir(compromisso, nameof(Compromisso.Id), 100);
        Definir(compromisso, nameof(Compromisso.Predio), new Predio("Edifício Aurora", "Rua X, 100", UsuarioId));
        Definir(compromisso, nameof(Compromisso.Responsavel), new Usuario("Pedro", "pedro@x.com", "hash"));

        _compromissoRepository
            .Setup(r => r.BuscarPorIdAsync(100, It.IsAny<CancellationToken>()))
            .ReturnsAsync(compromisso);
    }

    private static void Definir(object alvo, string propriedade, object valor) =>
        alvo.GetType().GetProperty(propriedade)!.SetValue(alvo, valor);
}
