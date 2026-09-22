import { describe, it, expect } from 'vitest'
import { predioNomeEhValido, enderecoEhValido } from './predioValidator'

describe('predioNomeEhValido', () => {
  it('aceita nomes legítimos de prédio, incluindo números e pontuação comum', () => {
    expect(predioNomeEhValido('Edifício Solar II')).toBe(true)
    expect(predioNomeEhValido('Bloco A - Torre 2')).toBe(true)
    expect(predioNomeEhValido('Residencial 9 de Julho')).toBe(true)
  })

  it('rejeita caracteres fora da allowlist (defesa em profundidade contra XSS)', () => {
    expect(predioNomeEhValido('<script>alert(1)</script>')).toBe(false)
  })
})

describe('enderecoEhValido', () => {
  it('aceita endereços legítimos, com vírgula e abreviações', () => {
    expect(enderecoEhValido('Rua da Silva, 192, Floresta')).toBe(true)
    expect(enderecoEhValido('Av. Brasil, Nº 100, Apto 12')).toBe(true)
  })

  it('rejeita caracteres fora da allowlist (defesa em profundidade contra XSS)', () => {
    expect(enderecoEhValido('<script>alert(1)</script>')).toBe(false)
  })
})
