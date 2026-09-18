using euSindico.Domain.Enums;

namespace euSindico.Application.Predios.Dtos;

// Papel é o vínculo do usuário autenticado com este prédio (Síndico, Gestor ou Colaborador) —
// o frontend usa isso para decidir, por prédio, se mostra as ações de editar/excluir/gerenciar
// equipe (exclusivas de Síndico) ou só as de navegação (PREDIOS.md, Fluxo 3).
public record PredioDto(int Id, string Nome, string Endereco, DateTime CriadoEm, PapelPredio Papel);
