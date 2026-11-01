namespace euSindico.Application.Common.Exceptions;

/// <summary>Lançada quando o prédio já enviou o máximo de atas no mês corrente (RN41).</summary>
public class LimiteAtasMensalAtingidoException(int limite)
    : Exception($"O limite de {limite} atas por mês foi atingido para este prédio. Exclua uma ata deste mês para enviar outra ou aguarde o próximo mês.");
