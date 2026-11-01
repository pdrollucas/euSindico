namespace euSindico.Application.Documentos.Dtos;

public record DocumentoArquivoDto(string NomeArquivo, string ContentType, Stream Conteudo);
