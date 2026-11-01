import { describe, it, expect } from 'vitest'
import {
  documentoSchema,
  documentosPaginadosSchema,
  enviarDocumentoFormSchema,
} from './documento.schema'
import { MENSAGEM_FORMATO, MENSAGEM_VAZIO } from '@/utils/arquivoDocumento'

const dto = {
  id: 1,
  nomeArquivo: 'Ata 03.pdf',
  predioId: 4,
  predioNome: 'Edifício Solar',
  criadoEm: '2026-10-01T12:00:00',
}

describe('enviarDocumentoFormSchema', () => {
  it('aceita um arquivo válido', () => {
    const arquivo = new File(['%PDF-1.4'], 'ata.pdf', { type: 'application/pdf' })

    expect(enviarDocumentoFormSchema.safeParse({ arquivo }).success).toBe(true)
  })

  it('exige o arquivo', () => {
    const resultado = enviarDocumentoFormSchema.safeParse({})

    expect(resultado.success).toBe(false)
    expect(resultado.error?.issues[0]?.message).toBe('Selecione um arquivo')
  })

  it('recusa valor que não é um File', () => {
    expect(enviarDocumentoFormSchema.safeParse({ arquivo: 'ata.pdf' }).success).toBe(false)
  })

  it('recusa formato fora de RN12 com a mensagem do backend', () => {
    const arquivo = new File(['x'], 'planilha.xlsx')

    const resultado = enviarDocumentoFormSchema.safeParse({ arquivo })

    expect(resultado.error?.issues[0]?.message).toBe(MENSAGEM_FORMATO)
  })

  it('recusa arquivo vazio', () => {
    const arquivo = new File([], 'ata.pdf')

    const resultado = enviarDocumentoFormSchema.safeParse({ arquivo })

    expect(resultado.error?.issues[0]?.message).toBe(MENSAGEM_VAZIO)
  })
})

describe('documentoSchema', () => {
  it('aceita o DocumentoDto do backend', () => {
    expect(() => documentoSchema.parse(dto)).not.toThrow()
  })

  it('rejeita payload com campo faltando (API mudou o contrato)', () => {
    const { nomeArquivo: _ignorado, ...semNome } = dto

    expect(() => documentoSchema.parse(semNome)).toThrow(/nomeArquivo/)
  })
})

describe('documentosPaginadosSchema', () => {
  it('aceita o PagedResultDto', () => {
    const pagina = { items: [dto], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 }

    expect(documentosPaginadosSchema.parse(pagina).items).toHaveLength(1)
  })

  it('rejeita item inválido dentro da lista', () => {
    const pagina = { items: [{ id: 1 }], page: 1, pageSize: 20, totalCount: 1, totalPages: 1 }

    expect(() => documentosPaginadosSchema.parse(pagina)).toThrow(/items/)
  })
})
