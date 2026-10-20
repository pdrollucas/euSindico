import { describe, it, expect } from 'vitest'
import { tituloEhValido, prestadorEhValido, detalhesEhValido } from './compromissoValidator'

describe('tituloEhValido', () => {
  it('aceita a pontuação comum de uma anotação', () => {
    expect(tituloEhValido('Reunião com a síndica - 10:00')).toBe(true)
    expect(tituloEhValido('Revisão (elevador) nº 2')).toBe(true)
  })

  it('rejeita < e >, aspas e chaves', () => {
    expect(tituloEhValido('<script>alert(1)</script>')).toBe(false)
    expect(tituloEhValido('Título "com aspas"')).toBe(false)
    expect(tituloEhValido('{titulo}')).toBe(false)
  })
})

describe('prestadorEhValido', () => {
  it('aceita nome de empresa', () => {
    expect(prestadorEhValido('Elevadores Atlas Ltda.')).toBe(true)
    expect(prestadorEhValido('J&J Pintura')).toBe(true)
  })

  it('rejeita < e >', () => {
    expect(prestadorEhValido('<b>Atlas</b>')).toBe(false)
  })
})

describe('detalhesEhValido', () => {
  it('aceita texto livre legítimo', () => {
    expect(detalhesEhValido('Orçamento: R$ 1.200,00\nContato: joao@x.com (50%)')).toBe(true)
  })

  it('rejeita < e >', () => {
    expect(detalhesEhValido('olá <b>mundo</b>')).toBe(false)
  })
})
