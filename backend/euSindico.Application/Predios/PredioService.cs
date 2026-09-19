using euSindico.Application.Common.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Application.Predios.Dtos;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;

namespace euSindico.Application.Predios;

public class PredioService(
    IPredioRepository predioRepository,
    IPredioUsuarioRepository predioUsuarioRepository,
    AutorizacaoPredioService autorizacaoPredioService)
{
    // Regra não prevista no RFC, decidida em PREDIOS.md — prédios excluídos não contam.
    private const int LimiteDePrediosAtivos = 20;

    public async Task<PredioDto> CriarAsync(int usuarioId, PredioFormDto dto, CancellationToken ct = default)
    {
        var nome = dto.Nome.Trim();
        var endereco = dto.Endereco.Trim();

        if (await predioRepository.ContarAtivosDoUsuarioAsync(usuarioId, ct) >= LimiteDePrediosAtivos)
        {
            throw new PredioLimiteAtingidoException();
        }

        if (await predioRepository.ExisteNomeEEnderecoAtivoAsync(usuarioId, nome, endereco, ct: ct))
        {
            throw new PredioDuplicadoException();
        }

        var predio = new Predio(nome, endereco, usuarioId);
        await predioRepository.AdicionarComDonoAsync(predio, ct);

        return new PredioDto(predio.Id, predio.Nome, predio.Endereco, predio.CriadoEm, PapelPredio.Sindico);
    }

    // Visualizar é permitido a qualquer papel vinculado (Síndico, Gestor ou Colaborador) —
    // RF09, PREDIOS.md Fluxo 3. Sem vínculo ou prédio excluído: PredioNaoEncontradoException
    // (mesma resposta anti-enumeração, RN08 trata "excluído" como "não existe").
    public async Task<PredioDto> ObterAsync(int usuarioId, int predioId, CancellationToken ct = default)
    {
        var vinculo = await autorizacaoPredioService.VerificarAcessoAsync(usuarioId, predioId, AcaoPredio.VisualizarPredio, ct);

        var predio = await predioRepository.BuscarPorIdAsync(predioId, ct);
        if (predio is null || predio.Excluido)
        {
            throw new PredioNaoEncontradoException();
        }

        return new PredioDto(predio.Id, predio.Nome, predio.Endereco, predio.CriadoEm, vinculo.Papel);
    }

    // Lista por vínculo (predio_usuarios), não por criação — inclui prédios onde o usuário é
    // Gestor ou Colaborador, não só os que ele criou (RF09, RN16, PREDIOS.md Fluxo 2).
    public async Task<PagedResultDto<PredioDto>> ListarAsync(int usuarioId, int page, int pageSize, CancellationToken ct = default)
    {
        var (itens, total) = await predioUsuarioRepository.ListarPrediosDoUsuarioAsync(usuarioId, page, pageSize, ct);

        var dtos = itens
            .Select(pu => new PredioDto(pu.Predio!.Id, pu.Predio.Nome, pu.Predio.Endereco, pu.Predio.CriadoEm, pu.Papel))
            .ToList();

        return new PagedResultDto<PredioDto>(dtos, page, pageSize, total);
    }

    // Exclusiva do papel Síndico (RF10, RF33, PREDIOS.md Fluxo 4). A checagem de duplicidade
    // continua escopada ao dono: só o Síndico chega até aqui, e Síndico só existe como dono do
    // próprio prédio (convite nunca atribui esse papel — PredioUsuario.CriarComoConvidado
    // rejeita PapelPredio.Sindico), então usuarioId já equivale a Predio.UsuarioId.
    public async Task<PredioDto> AtualizarAsync(int usuarioId, int predioId, PredioFormDto dto, CancellationToken ct = default)
    {
        var vinculo = await autorizacaoPredioService.VerificarAcessoAsync(usuarioId, predioId, AcaoPredio.GerenciarPredio, ct);

        var predio = await predioRepository.BuscarPorIdAsync(predioId, ct);
        if (predio is null || predio.Excluido)
        {
            throw new PredioNaoEncontradoException();
        }

        var nome = dto.Nome.Trim();
        var endereco = dto.Endereco.Trim();

        if (await predioRepository.ExisteNomeEEnderecoAtivoAsync(usuarioId, nome, endereco, predioId, ct))
        {
            throw new PredioDuplicadoException();
        }

        predio.AtualizarDados(nome, endereco);
        await predioRepository.AtualizarAsync(predio, ct);

        return new PredioDto(predio.Id, predio.Nome, predio.Endereco, predio.CriadoEm, vinculo.Papel);
    }

    // Soft delete, exclusiva do papel Síndico (RF11, RN07, RN08, PREDIOS.md Fluxo 5). Diferente
    // do logout: um prédio já excluído retorna 404, não 204 — não há "estado desejado" implícito
    // em excluir de novo algo que já não existe (RN08).
    public async Task ExcluirAsync(int usuarioId, int predioId, CancellationToken ct = default)
    {
        await autorizacaoPredioService.VerificarAcessoAsync(usuarioId, predioId, AcaoPredio.GerenciarPredio, ct);

        var predio = await predioRepository.BuscarPorIdAsync(predioId, ct);
        if (predio is null || predio.Excluido)
        {
            throw new PredioNaoEncontradoException();
        }

        predio.ExcluirLogicamente();
        await predioRepository.AtualizarAsync(predio, ct);
    }
}
