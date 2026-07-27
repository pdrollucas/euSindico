import { describe, it, expect } from 'vitest'
import { segundosRestantes, formatarContagem } from './tempo'

describe('segundosRestantes', () => {
  it('retorna 0 quando não há expiração (null)', () => {
    expect(segundosRestantes(null, 1000)).toBe(0)
  })

  it('retorna os segundos restantes, arredondando para cima', () => {
    expect(segundosRestantes(10_500, 0)).toBe(11)
    expect(segundosRestantes(300_000, 0)).toBe(300)
  })

  it('nunca retorna negativo quando a expiração já passou', () => {
    expect(segundosRestantes(1000, 5000)).toBe(0)
  })
})

describe('formatarContagem', () => {
  it('formata segundos como m:ss', () => {
    expect(formatarContagem(0)).toBe('0:00')
    expect(formatarContagem(9)).toBe('0:09')
    expect(formatarContagem(65)).toBe('1:05')
    expect(formatarContagem(300)).toBe('5:00')
  })

  it('trata valores negativos como 0', () => {
    expect(formatarContagem(-5)).toBe('0:00')
  })
})
