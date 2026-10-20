using euSindico.Application.Common.Dtos;
using euSindico.Application.Common.Exceptions;
using euSindico.Application.Common.Interfaces;
using euSindico.Application.Compromissos.Dtos;
using euSindico.Application.Equipe;
using euSindico.Domain.Entities;
using euSindico.Domain.Enums;

namespace euSindico.Application.Compromissos;

public class CompromissoService(
    ICompromissoRepository compromissoRepository,
    IPredioUsuarioRepository predioUsuarioRepository,
    AutorizacaoPredioService autorizacaoPredioService,
    TimeProvider timeProvider)
{
    // RF12, RF13, RN03, RN09, RN10, RN20 — COMPROMISSOS.md, Fluxo 1.
    public async Task<CompromissoDto> CriarAsync(int usuarioId, CompromissoFormDto dto, CancellationToken ct = default)
    {
        var vinculo = await AcessarPredioAsync(usuarioId, dto.PredioId!.Value, ct);

        int responsavelId;
        string responsavelNome;

        if (vinculo.Papel == PapelPredio.Colaborador)
        {
            // RN20: o Colaborador é sempre o próprio responsável; indicar outro é tentativa de reatribuição.
            if (dto.ResponsavelUsuarioId is not null && dto.ResponsavelUsuarioId != usuarioId)
            {
                throw new AcaoNaoPermitidaException();
            }

            responsavelId = usuarioId;
            responsavelNome = vinculo.Usuario!.Nome;
        }
        else
        {
            responsavelId = dto.ResponsavelUsuarioId ?? usuarioId;
            responsavelNome = responsavelId == usuarioId
                ? vinculo.Usuario!.Nome
                : (await ValidarResponsavelAsync(dto.PredioId!.Value, responsavelId, ct)).Usuario!.Nome;
        }

        var compromisso = new Compromisso(
            dto.Titulo.Trim(),
            dto.DataCompromisso!.Value,
            dto.HorarioCompromisso!.Value,
            dto.PredioId!.Value,
            responsavelId,
            NormalizarOpcional(dto.NomePrestadorServico),
            NormalizarOpcional(dto.Detalhes));

        await compromissoRepository.AdicionarAsync(compromisso, ct);

        return ParaDto(compromisso, vinculo.Predio!.Nome, responsavelNome);
    }

    // RF14, RF34, RN14, RN16, RN21 — COMPROMISSOS.md, Fluxo 2. Lista por vínculo em todos os
    // prédios do usuário; com predioId, restringe a um (e exige acesso a ele).
    public async Task<PagedResultDto<CompromissoDto>> ListarAsync(
        int usuarioId,
        string? titulo,
        int? predioId,
        EscopoCompromissos escopo,
        SituacaoCompromissos situacao,
        int page,
        int pageSize,
        CancellationToken ct = default)
    {
        if (predioId is not null)
        {
            await AcessarPredioAsync(usuarioId, predioId.Value, ct);
        }

        var agora = timeProvider.GetLocalNow().DateTime;
        var filtro = new CompromissoFiltro(
            usuarioId,
            NormalizarOpcional(titulo),
            predioId,
            escopo,
            situacao,
            DateOnly.FromDateTime(agora),
            TimeOnly.FromDateTime(agora));

        var (itens, total) = await compromissoRepository.ListarAsync(filtro, page, pageSize, ct);

        return new PagedResultDto<CompromissoDto>(itens.Select(c => ParaDto(c)).ToList(), page, pageSize, total);
    }

    public async Task<CompromissoDto> ObterAsync(int usuarioId, int compromissoId, CancellationToken ct = default)
    {
        var (compromisso, _) = await CarregarComAcessoAsync(usuarioId, compromissoId, ct);
        return ParaDto(compromisso);
    }

    // RF15, RN09, RN20 — COMPROMISSOS.md, Fluxo 4. Compromisso concluído pode ser editado.
    public async Task<CompromissoDto> AtualizarAsync(
        int usuarioId, int compromissoId, CompromissoFormDto dto, CancellationToken ct = default)
    {
        var (compromisso, vinculoOrigem) = await CarregarComAcessoAsync(usuarioId, compromissoId, ct);
        var papel = vinculoOrigem.Papel;

        var mudouPredio = dto.PredioId!.Value != compromisso.PredioId;
        var vinculoDestino = vinculoOrigem;

        if (mudouPredio)
        {
            // Só Síndico/Gestor movem, e precisam de um desses papéis também no prédio de destino.
            if (papel == PapelPredio.Colaborador)
            {
                throw new AcaoNaoPermitidaException();
            }

            vinculoDestino = await AcessarPredioAsync(usuarioId, dto.PredioId!.Value, ct);
            if (vinculoDestino.Papel == PapelPredio.Colaborador)
            {
                throw new AcaoNaoPermitidaException();
            }
        }

        var responsavelId = compromisso.ResponsavelUsuarioId;
        var responsavelNome = compromisso.Responsavel!.Nome;

        if (papel == PapelPredio.Colaborador)
        {
            if (dto.ResponsavelUsuarioId is not null && dto.ResponsavelUsuarioId != responsavelId)
            {
                throw new AcaoNaoPermitidaException();
            }
        }
        else
        {
            // Omitido na edição = mantém o atual. Só revalida contra a equipe quando muda de
            // responsável ou de prédio — senão um compromisso cujo responsável já saiu da equipe
            // (RN22) ficaria impossível de editar.
            var novoResponsavelId = dto.ResponsavelUsuarioId ?? responsavelId;
            if (novoResponsavelId != responsavelId || mudouPredio)
            {
                var vinculoResponsavel = await ValidarResponsavelAsync(dto.PredioId!.Value, novoResponsavelId, ct);
                responsavelId = novoResponsavelId;
                responsavelNome = vinculoResponsavel.Usuario!.Nome;
            }
        }

        compromisso.AtualizarDados(
            dto.Titulo.Trim(),
            dto.DataCompromisso!.Value,
            dto.HorarioCompromisso!.Value,
            NormalizarOpcional(dto.NomePrestadorServico),
            NormalizarOpcional(dto.Detalhes),
            dto.PredioId!.Value,
            responsavelId);

        await compromissoRepository.AtualizarAsync(compromisso, ct);

        return ParaDto(compromisso, vinculoDestino.Predio!.Nome, responsavelNome);
    }

    // RF17 — COMPROMISSOS.md, Fluxo 5. Idempotente e reversível: informa o estado desejado.
    public async Task<CompromissoDto> AlterarConclusaoAsync(
        int usuarioId, int compromissoId, bool concluido, CancellationToken ct = default)
    {
        var (compromisso, _) = await CarregarComAcessoAsync(usuarioId, compromissoId, ct);

        if (concluido)
        {
            compromisso.Concluir();
        }
        else
        {
            compromisso.Reabrir();
        }

        await compromissoRepository.AtualizarAsync(compromisso, ct);

        return ParaDto(compromisso);
    }

    // RF16 — exclusão física, COMPROMISSOS.md, Fluxo 6.
    public async Task ExcluirAsync(int usuarioId, int compromissoId, CancellationToken ct = default)
    {
        var (compromisso, _) = await CarregarComAcessoAsync(usuarioId, compromissoId, ct);
        await compromissoRepository.RemoverAsync(compromisso, ct);
    }

    // Vínculo (RN16) + prédio não excluído (RN08, RN09). Qualquer papel serve — o que cada um
    // pode fazer com o compromisso em si é decidido pelo chamador (RN20, RN21).
    private async Task<PredioUsuario> AcessarPredioAsync(int usuarioId, int predioId, CancellationToken ct)
    {
        var vinculo = await autorizacaoPredioService.VerificarAcessoAsync(
            usuarioId, predioId, AcaoPredio.GerenciarCompromissos, ct);

        if (vinculo.Predio is null || vinculo.Predio.Excluido)
        {
            throw new PredioNaoEncontradoException();
        }

        return vinculo;
    }

    // Compromisso inexistente, sem vínculo com o prédio e prédio excluído devolvem a mesma
    // resposta (404 "Compromisso não encontrado") — o id não revela se existe. Já o Colaborador
    // que tenta mexer num compromisso alheio recebe 403 (RN21): ele é membro do prédio e já sabe que existe.
    private async Task<(Compromisso Compromisso, PredioUsuario Vinculo)> CarregarComAcessoAsync(
        int usuarioId, int compromissoId, CancellationToken ct)
    {
        var compromisso = await compromissoRepository.BuscarPorIdAsync(compromissoId, ct)
            ?? throw new CompromissoNaoEncontradoException();

        PredioUsuario vinculo;
        try
        {
            vinculo = await AcessarPredioAsync(usuarioId, compromisso.PredioId, ct);
        }
        catch (PredioNaoEncontradoException)
        {
            throw new CompromissoNaoEncontradoException();
        }

        if (vinculo.Papel == PapelPredio.Colaborador && compromisso.ResponsavelUsuarioId != usuarioId)
        {
            throw new AcaoNaoPermitidaException();
        }

        return (compromisso, vinculo);
    }

    private async Task<PredioUsuario> ValidarResponsavelAsync(int predioId, int responsavelId, CancellationToken ct) =>
        await predioUsuarioRepository.BuscarVinculoAsync(predioId, responsavelId, ct)
            ?? throw new ResponsavelInvalidoException();

    private static string? NormalizarOpcional(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static CompromissoDto ParaDto(Compromisso c) =>
        ParaDto(c, c.Predio!.Nome, c.Responsavel!.Nome);

    private static CompromissoDto ParaDto(Compromisso c, string predioNome, string responsavelNome) =>
        new(
            c.Id,
            c.Titulo,
            c.DataCompromisso,
            c.HorarioCompromisso,
            c.NomePrestadorServico,
            c.Detalhes,
            c.Concluido,
            c.PredioId,
            predioNome,
            c.ResponsavelUsuarioId,
            responsavelNome,
            c.CriadoEm);
}
