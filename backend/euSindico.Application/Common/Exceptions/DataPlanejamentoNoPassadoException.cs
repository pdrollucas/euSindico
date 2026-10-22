namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Lançada quando a data de um planejamento futuro é anterior a hoje (fuso da aplicação).
/// Diferente de compromissos (RN26), planejamento é para o que ainda vai acontecer.
/// </summary>
public class DataPlanejamentoNoPassadoException() : Exception("A data do planejamento não pode estar no passado.");
