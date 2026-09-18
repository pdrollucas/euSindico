using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Equipe;
using euSindico.Application.Predios.Dtos;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;

namespace euSindico.Application.Predios;

public class PredioService(IPredioRepository predioRepository, AutorizacaoPredioService autorizacaoPredioService)
{
    // Regra não prevista no RFC, decidida em PREDIOS.md — prédios excluídos não contam.
    private const int LimiteDePrediosAtivos = 20;

    public async Task<PredioDto> CriarAsync(int usuarioId, CriarPredioDto dto, CancellationToken ct = default)
    {
        var nome = dto.Nome.Trim();
        var endereco = dto.Endereco.Trim();

        if (await predioRepository.ContarAtivosDoUsuarioAsync(usuarioId, ct) >= LimiteDePrediosAtivos)
        {
            throw new PredioLimiteAtingidoException();
        }

        if (await predioRepository.ExisteNomeEEnderecoAtivoAsync(usuarioId, nome, endereco, ct))
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
}
