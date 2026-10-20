namespace euSindico.Infrastructure.Time;

/// <summary>
/// Relógio do sistema com o fuso da aplicação, não o do servidor (que em App Runner/Docker é UTC).
/// "Hoje" e "atrasado" nos compromissos dependem disso: data/horário do compromisso não têm fuso,
/// são sempre o horário de parede do síndico.
/// </summary>
public class FusoHorarioTimeProvider(TimeZoneInfo fuso) : TimeProvider
{
    public const string FusoPadrao = "America/Sao_Paulo";

    public override TimeZoneInfo LocalTimeZone => fuso;
}
