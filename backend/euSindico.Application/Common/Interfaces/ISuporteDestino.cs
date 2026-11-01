namespace euSindico.Application.Common.Interfaces;

public interface ISuporteDestino
{
    /// <summary>Caixa que recebe as mensagens enviadas pela tela de suporte.</summary>
    string EmailDestino { get; }
}
