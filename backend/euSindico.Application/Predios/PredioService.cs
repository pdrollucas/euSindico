using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Predios.Dtos;
using euSindico.Domain.Entities;

namespace euSindico.Application.Predios;

public class PredioService(IPredioRepository predioRepository)
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

        return new PredioDto(predio.Id, predio.Nome, predio.Endereco, predio.CriadoEm);
    }
}
