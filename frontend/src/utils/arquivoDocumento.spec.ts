import { describe, it, expect } from 'vitest'
import {
  abreNoNavegador,
  categoriaDe,
  contentTypeDe,
  extensaoDe,
  MENSAGEM_FORMATO,
  MENSAGEM_NOME,
  MENSAGEM_TAMANHO,
  MENSAGEM_VAZIO,
  NOME_MAXIMO,
  TAMANHO_MAXIMO_BYTES,
  validarArquivoDocumento,
} from './arquivoDocumento'

const arquivo = (name: string, size = 1024) => ({ name, size })

describe('validarArquivoDocumento', () => {
  it.each(['ata.pdf', 'regimento.docx', 'foto.jpg', 'foto.jpeg', 'imagem.png', 'ATA.PDF'])(
    'aceita %s',
    (nome) => {
      expect(validarArquivoDocumento(arquivo(nome))).toBeNull()
    },
  )

  it.each(['planilha.xlsx', 'antigo.doc', 'virus.exe', 'pagina.html', 'texto.txt', 'semextensao'])(
    'recusa o formato de %s (RN12: sem planilha nem .doc)',
    (nome) => {
      expect(validarArquivoDocumento(arquivo(nome))).toBe(MENSAGEM_FORMATO)
    },
  )

  it('aceita nomes com acento e pontuação comum', () => {
    expect(validarArquivoDocumento(arquivo('Ata nº 3 (assembleia) - 2026.pdf'))).toBeNull()
    expect(validarArquivoDocumento(arquivo('Convocação & pauta, março.pdf'))).toBeNull()
  })

  it.each([
    '<script>alert(1)</script>.pdf',
    'a"b.pdf',
    'pasta/ata.pdf',
    'pasta\\ata.pdf',
    '..\\ata.pdf',
    'ata:stream.pdf',
    'ata?.pdf',
    'ata*.pdf',
    'ata\n.pdf',
    '.pdf',
    '',
    '   ',
  ])('recusa o nome inválido %j', (nome) => {
    expect(validarArquivoDocumento(arquivo(nome))).toBe(MENSAGEM_NOME)
  })

  it('recusa nome com mais de 255 caracteres', () => {
    const nome = `${'a'.repeat(NOME_MAXIMO - 3)}.pdf`

    expect(validarArquivoDocumento(arquivo(nome))).toBe(MENSAGEM_NOME)
  })

  it('aceita nome com exatamente 255 caracteres', () => {
    const nome = `${'a'.repeat(NOME_MAXIMO - 4)}.pdf`

    expect(validarArquivoDocumento(arquivo(nome))).toBeNull()
  })

  it('recusa arquivo vazio', () => {
    expect(validarArquivoDocumento(arquivo('ata.pdf', 0))).toBe(MENSAGEM_VAZIO)
  })

  it('aceita exatamente 20 MB e recusa acima', () => {
    expect(validarArquivoDocumento(arquivo('ata.pdf', TAMANHO_MAXIMO_BYTES))).toBeNull()
    expect(validarArquivoDocumento(arquivo('ata.pdf', TAMANHO_MAXIMO_BYTES + 1))).toBe(
      MENSAGEM_TAMANHO,
    )
  })

  it('confere o nome antes do formato, e o formato antes do tamanho (mesma ordem do backend)', () => {
    expect(validarArquivoDocumento(arquivo('a/b.exe', 0))).toBe(MENSAGEM_NOME)
    expect(validarArquivoDocumento(arquivo('a.exe', TAMANHO_MAXIMO_BYTES + 1))).toBe(
      MENSAGEM_FORMATO,
    )
  })

  it('apara os espaços das pontas do nome antes de conferir', () => {
    expect(validarArquivoDocumento(arquivo('  ata.pdf  '))).toBeNull()
  })
})

describe('extensaoDe', () => {
  it('devolve a extensão em minúsculas, com ponto', () => {
    expect(extensaoDe('Ata.PDF')).toBe('.pdf')
    expect(extensaoDe('a.b.docx')).toBe('.docx')
  })

  it('devolve vazio sem extensão', () => {
    expect(extensaoDe('semextensao')).toBe('')
  })
})

describe('categoriaDe / abreNoNavegador', () => {
  it.each([
    ['ata.pdf', 'pdf', true],
    ['foto.jpg', 'imagem', true],
    ['foto.JPEG', 'imagem', true],
    ['foto.png', 'imagem', true],
    ['regimento.docx', 'docx', false],
    ['outro.bin', 'outro', false],
  ] as const)('%s é %s e abre no navegador: %s', (nome, categoria, abre) => {
    expect(categoriaDe(nome)).toBe(categoria)
    expect(abreNoNavegador(nome)).toBe(abre)
  })
})

describe('contentTypeDe', () => {
  it('deriva o tipo da extensão', () => {
    expect(contentTypeDe('ata.pdf')).toBe('application/pdf')
    expect(contentTypeDe('foto.JPG')).toBe('image/jpeg')
    expect(contentTypeDe('foto.png')).toBe('image/png')
    expect(contentTypeDe('regimento.docx')).toBe(
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document',
    )
  })

  it('usa octet-stream para o desconhecido (nunca um tipo que o navegador renderize)', () => {
    expect(contentTypeDe('estranho.html')).toBe('application/octet-stream')
  })
})
