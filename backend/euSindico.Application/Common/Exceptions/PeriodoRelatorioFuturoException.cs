namespace euSindico.Application.Common.Exceptions;

/// <summary>Lançada ao pedir um relatório de um mês que ainda não começou (fuso da aplicação).</summary>
public class PeriodoRelatorioFuturoException() : Exception("Não é possível gerar relatório de um mês futuro.");
