namespace euSindico.Application.Compromissos;

/// <summary>
/// Filtro "meus" x "todos" da tela de compromissos (RF34). Colaborador nunca enxerga além dos
/// próprios (RN21), qualquer que seja o valor enviado.
/// </summary>
public enum EscopoCompromissos
{
    Meus,
    Todos,
}
