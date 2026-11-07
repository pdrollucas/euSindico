// Regras de arquivo de atas e normas (RN12, RN38). Espelham ArquivoDocumento do backend para dar
// feedback antes do upload — é UX, não segurança: o backend revalida tudo, inclusive o conteúdo
// (assinatura do arquivo), que o frontend não confere (ver SECURITY.md, seção 5).

export const TAMANHO_MAXIMO_BYTES = 20 * 1024 * 1024
export const NOME_MAXIMO = 255

export const EXTENSOES_ACEITAS = ['.pdf', '.docx', '.jpg', '.jpeg', '.png'] as const
// Valor do atributo `accept` do seletor de arquivo (só filtra o diálogo do sistema, não valida).
export const ACCEPT_ARQUIVO = EXTENSOES_ACEITAS.join(',')
export const FORMATOS_ACEITOS_TEXTO = 'PDF, DOCX, JPG ou PNG'

export const MENSAGEM_NOME = 'O nome do arquivo é inválido.'
export const MENSAGEM_FORMATO = `Formato de arquivo não aceito. Envie ${FORMATOS_ACEITOS_TEXTO}.`
export const MENSAGEM_VAZIO = 'O arquivo está vazio.'
export const MENSAGEM_TAMANHO = `O arquivo deve ter no máximo ${TAMANHO_MAXIMO_BYTES / (1024 * 1024)} MB.`

// Caracteres que não fazem parte de um nome legítimo (< > aspas crase chaves | barras : * ?).
const NOME_PROIBIDO = /[<>"`{}|\\/:*?]/

function temCaractereDeControle(texto: string): boolean {
  return [...texto].some((c) => {
    const codigo = c.charCodeAt(0)
    return codigo < 32 || (codigo >= 127 && codigo <= 159)
  })
}

// ".pdf" em minúsculas; vazio quando o nome não tem extensão.
export function extensaoDe(nomeArquivo: string): string {
  const indice = nomeArquivo.lastIndexOf('.')
  return indice < 0 ? '' : nomeArquivo.slice(indice).toLowerCase()
}

// Mensagem do primeiro problema encontrado, ou `null` se o arquivo pode ser enviado. Mesma ordem
// de checagens do backend: nome, formato, vazio, tamanho.
export function validarArquivoDocumento(arquivo: { name: string; size: number }): string | null {
  const nome = arquivo.name.trim()

  if (
    nome.length === 0 ||
    nome.length > NOME_MAXIMO ||
    nome.startsWith('.') ||
    NOME_PROIBIDO.test(nome) ||
    temCaractereDeControle(nome)
  ) {
    return MENSAGEM_NOME
  }

  if (!(EXTENSOES_ACEITAS as readonly string[]).includes(extensaoDe(nome))) {
    return MENSAGEM_FORMATO
  }

  if (arquivo.size === 0) {
    return MENSAGEM_VAZIO
  }

  if (arquivo.size > TAMANHO_MAXIMO_BYTES) {
    return MENSAGEM_TAMANHO
  }

  return null
}

export type CategoriaArquivo = 'pdf' | 'imagem' | 'docx' | 'outro'

export function categoriaDe(nomeArquivo: string): CategoriaArquivo {
  switch (extensaoDe(nomeArquivo)) {
    case '.pdf':
      return 'pdf'
    case '.jpg':
    case '.jpeg':
    case '.png':
      return 'imagem'
    case '.docx':
      return 'docx'
    default:
      return 'outro'
  }
}

const CONTENT_TYPES: Record<string, string> = {
  '.pdf': 'application/pdf',
  '.docx': 'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
  '.jpg': 'image/jpeg',
  '.jpeg': 'image/jpeg',
  '.png': 'image/png',
}

// O tipo do Blob baixado vem da extensão do nome (já validada pelo backend contra o conteúdo), nunca
// do que a resposta declara: é ele que decide como o navegador trata o arquivo numa nova aba.
export function contentTypeDe(nomeArquivo: string): string {
  return CONTENT_TYPES[extensaoDe(nomeArquivo)] ?? 'application/octet-stream'
}

// Só PDF e imagem abrem numa nova aba; o navegador não renderiza DOCX, então o clique baixa.
export function abreNoNavegador(nomeArquivo: string): boolean {
  const categoria = categoriaDe(nomeArquivo)
  return categoria === 'pdf' || categoria === 'imagem'
}

const ICONES_POR_CATEGORIA: Record<CategoriaArquivo, string> = {
  pdf: 'mdi-file-pdf-box',
  imagem: 'mdi-file-image-outline',
  docx: 'mdi-file-word-outline',
  outro: 'mdi-file-outline',
}

export function iconeDoArquivo(nomeArquivo: string): string {
  return ICONES_POR_CATEGORIA[categoriaDe(nomeArquivo)]
}
