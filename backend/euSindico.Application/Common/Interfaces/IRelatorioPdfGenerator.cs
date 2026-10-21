using euSindico.Application.Relatorios;

namespace euSindico.Application.Common.Interfaces;

public interface IRelatorioPdfGenerator
{
    byte[] Gerar(DadosRelatorio dados);
}
