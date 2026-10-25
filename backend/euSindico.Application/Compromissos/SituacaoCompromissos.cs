namespace euSindico.Application.Compromissos;

/// <summary>
/// Filtro de situação da listagem de compromissos (RFC 3.1). "Hoje" e "agora" seguem o fuso
/// configurado da aplicação, não o do servidor.
/// </summary>
public enum SituacaoCompromissos
{
    /// <summary>Padrão: não concluídos de hoje em diante.</summary>
    Proximos,

    /// <summary>Não concluídos com data igual a hoje.</summary>
    Hoje,

    /// <summary>Não concluídos cuja data/horário já passou.</summary>
    Atrasados,

    /// <summary>Todos os concluídos, em qualquer data.</summary>
    Concluidos,

    /// <summary>Sem filtro de situação: concluídos ou não, em qualquer data (o período, se houver, ainda vale).</summary>
    Todos,
}
