using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using euSindico.Application.Compromissos;
using euSindico.Application.Compromissos.Dtos;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace euSindico.Api.Controllers;

[ApiController]
[Authorize]
[Route("compromissos")]
public class CompromissoController(
    CompromissoService compromissoService,
    IValidator<CompromissoFormDto> formValidator,
    IValidator<AlterarConclusaoDto> conclusaoValidator) : ControllerBase
{
    private const int PageSizeMaximo = 20;
    private const int TituloFiltroMaximo = 150;
    private const int AnoFiltroMinimo = 2000;
    private const int AnoFiltroMaximo = 2100;

    // O id do usuário vem sempre da claim do token (sub), nunca de um parâmetro de rota/query (RN02).
    private int UsuarioId => int.Parse(User.FindFirstValue(JwtRegisteredClaimNames.Sub)!);

    [HttpPost]
    public async Task<IActionResult> Criar(CompromissoFormDto dto, CancellationToken ct)
    {
        var validationResult = await formValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var compromisso = await compromissoService.CriarAsync(UsuarioId, dto, ct);

        return CreatedAtAction(nameof(Obter), new { id = compromisso.Id }, compromisso);
    }

    // Sem período a lista cresce sem limite com o tempo de uso. Hoje já é um dia só e Atrasados é
    // o que o usuário precisa ver por inteiro, então só nelas o período é dispensável.
    private static bool PeriodoObrigatorio(SituacaoCompromissos situacao) =>
        situacao is SituacaoCompromissos.Proximos or SituacaoCompromissos.Concluidos or SituacaoCompromissos.Todos;

    [HttpGet]
    public async Task<IActionResult> Listar(
        string? titulo = null,
        int? predioId = null,
        EscopoCompromissos escopo = EscopoCompromissos.Meus,
        SituacaoCompromissos situacao = SituacaoCompromissos.Proximos,
        int page = 1,
        int pageSize = 10,
        int? ano = null,
        int? mes = null,
        CancellationToken ct = default)
    {
        var erros = new Dictionary<string, string[]>();

        if (page < 1)
        {
            erros[nameof(page)] = ["A página deve ser no mínimo 1."];
        }

        if (pageSize < 1 || pageSize > PageSizeMaximo)
        {
            erros[nameof(pageSize)] = [$"O tamanho da página deve estar entre 1 e {PageSizeMaximo}."];
        }

        if (titulo is { Length: > TituloFiltroMaximo })
        {
            erros[nameof(titulo)] = [$"A busca deve ter no máximo {TituloFiltroMaximo} caracteres."];
        }

        if (predioId is <= 0)
        {
            erros[nameof(predioId)] = ["O prédio informado é inválido."];
        }

        if (ano is { } a && (a < AnoFiltroMinimo || a > AnoFiltroMaximo))
        {
            erros[nameof(ano)] = [$"O ano deve estar entre {AnoFiltroMinimo} e {AnoFiltroMaximo}."];
        }

        if (mes is { } m && (m < 1 || m > 12))
        {
            erros[nameof(mes)] = ["O mês deve estar entre 1 e 12."];
        }
        else if ((ano is null) != (mes is null))
        {
            // Ano e mês andam juntos: não existe "o mês de setembro" sem dizer de qual ano, nem "o ano todo".
            erros[nameof(mes)] = ["Informe ano e mês juntos."];
        }
        else if (ano is null && PeriodoObrigatorio(situacao))
        {
            erros[nameof(mes)] = ["Informe ano e mês para esta situação."];
        }

        // O binder aceita números fora do enum (ex: situacao=9) — sem isso cairia no `_ => query` do repositório.
        if (!Enum.IsDefined(escopo))
        {
            erros[nameof(escopo)] = ["Escopo inválido. Use 'meus' ou 'todos'."];
        }

        if (!Enum.IsDefined(situacao))
        {
            erros[nameof(situacao)] = ["Situação inválida. Use 'proximos', 'hoje', 'atrasados', 'concluidos' ou 'todos'."];
        }

        if (erros.Count > 0)
        {
            return ValidationProblem(new ValidationProblemDetails(erros));
        }

        var resultado = await compromissoService.ListarAsync(UsuarioId, titulo, predioId, escopo, situacao, page, pageSize, ano, mes, ct);
        return Ok(resultado);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Obter(int id, CancellationToken ct)
    {
        var compromisso = await compromissoService.ObterAsync(UsuarioId, id, ct);
        return Ok(compromisso);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Atualizar(int id, CompromissoFormDto dto, CancellationToken ct)
    {
        var validationResult = await formValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var compromisso = await compromissoService.AtualizarAsync(UsuarioId, id, dto, ct);
        return Ok(compromisso);
    }

    // Marcar/desmarcar "concluído" (RF17). PATCH, não PUT: o checkbox da lista só informa o estado
    // desejado, sem reenviar (nem sobrescrever) os demais campos.
    [HttpPatch("{id:int}")]
    public async Task<IActionResult> AlterarConclusao(int id, AlterarConclusaoDto dto, CancellationToken ct)
    {
        var validationResult = await conclusaoValidator.ValidateAsync(dto, ct);
        if (!validationResult.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validationResult.ToDictionary()));
        }

        var compromisso = await compromissoService.AlterarConclusaoAsync(UsuarioId, id, dto.Concluido!.Value, ct);
        return Ok(compromisso);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Remover(int id, CancellationToken ct)
    {
        await compromissoService.ExcluirAsync(UsuarioId, id, ct);
        return NoContent();
    }
}
