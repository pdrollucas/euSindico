namespace euSindico.Domain.Enums;

/// <summary>
/// Perfil de acesso de um usuário a um prédio específico (RF29–RF34). O mesmo usuário
/// pode ter papéis diferentes em prédios diferentes — o papel vive no vínculo
/// (<see cref="Entities.PredioUsuario"/>), nunca no <see cref="Entities.Usuario"/> isolado.
///
/// Nomes genéricos de propósito (nível de permissão, não cargo real da pessoa) — o síndico
/// pode convidar qualquer tipo de funcionário (secretária, zelador, contador...) com o nível
/// de acesso que fizer sentido, sem o sistema forçar um rótulo de cargo específico.
///
/// Valores fixos e explícitos: nunca renumerar um papel existente ao adicionar um novo —
/// o valor numérico é o que fica persistido em <c>predio_usuarios.papel</c>/
/// <c>convites_funcionario.papel</c> (INT), então a ordem de declaração aqui não importa,
/// só o número atribuído a cada um.
/// </summary>
public enum PapelPredio
{
    Sindico = 1,
    Gestor = 2,
    Colaborador = 3,
}
