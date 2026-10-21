namespace euSindico.Application.Relatorios.Dtos;

// Anuláveis para um corpo vazio não virar mês/ano 0 em silêncio — o validator exige os dois.
public record GerarRelatorioDto(int? Mes, int? Ano);
