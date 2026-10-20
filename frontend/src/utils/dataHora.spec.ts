import { describe, it, expect } from 'vitest'
import { formatarData, formatarHorario, mascararHorario } from './dataHora'

describe('mascararHorario', () => {
  it('insere o ":" depois do segundo dígito', () => {
    expect(mascararHorario('1830')).toBe('18:30')
    expect(mascararHorario('183')).toBe('18:3')
  })

  it('não mostra o ":" antes do terceiro dígito (permite apagar com backspace)', () => {
    expect(mascararHorario('18')).toBe('18')
    expect(mascararHorario('18:')).toBe('18')
  })

  it('descarta letras e símbolos', () => {
    expect(mascararHorario('a1b8c:3d0')).toBe('18:30')
  })

  it('limita a 4 dígitos', () => {
    expect(mascararHorario('183099')).toBe('18:30')
  })

  it('aceita vazio e nulo (o X do campo)', () => {
    expect(mascararHorario('')).toBe('')
    expect(mascararHorario(null)).toBe('')
  })
})

describe('formatarData', () => {
  const hoje = new Date(2026, 9, 18)

  it('omite o ano quando é o ano corrente', () => {
    expect(formatarData('2026-02-18', hoje)).toBe('18/02')
  })

  it('inclui o ano quando é diferente do corrente', () => {
    expect(formatarData('2025-12-31', hoje)).toBe('31/12/2025')
  })
})

describe('formatarHorario', () => {
  it('descarta os segundos', () => {
    expect(formatarHorario('18:30:00')).toBe('18:30')
  })

  it('mantém um horário já sem segundos', () => {
    expect(formatarHorario('09:05')).toBe('09:05')
  })
})
