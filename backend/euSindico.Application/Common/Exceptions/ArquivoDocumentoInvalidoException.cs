namespace euSindico.Application.Common.Exceptions;

/// <summary>
/// Arquivo enviado fora das regras de RN12 (formato, tamanho, conteúdo incompatível com a extensão)
/// ou com nome inválido. A mensagem é mostrada ao usuário (fluxo "Falha no upload de documento", RFC 3.2).
/// </summary>
public class ArquivoDocumentoInvalidoException(string mensagem) : Exception(mensagem);
