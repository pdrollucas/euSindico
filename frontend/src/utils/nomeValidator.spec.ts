import { describe, it, expect } from 'vitest'
import { nomeEhValido } from './nomeValidator'

describe('nomeEhValido', () => {
  it('aceita nome com acentuação e espaço', () => {
    expect(nomeEhValido('João da Silva')).toBe(true)
  })

  it('aceita nome com hífen e apóstrofo', () => {
    expect(nomeEhValido("Maria D'Ávila-Souza")).toBe(true)
  })

  it('rejeita nome com números', () => {
    expect(nomeEhValido('João123')).toBe(false)
  })

  it('rejeita nome com símbolos não permitidos', () => {
    expect(nomeEhValido('João<script>')).toBe(false)
  })
})
