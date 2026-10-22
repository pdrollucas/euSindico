import { describe, it, expect } from 'vitest'
import {
  formatarData,
  formatarDataHoraUtc,
  formatarHorario,
  hojeIso,
  mascararHorario,
} from './dataHora'

describe('hojeIso', () => {
  it('formata a data local como yyyy-MM-dd, com zeros à esquerda', () => {
    expect(hojeIso(new Date(2026, 0, 5, 12))).toBe('2026-01-05')
  })

  it('usa o dia local mesmo perto da meia-noite (sem passar por UTC)', () => {
    expect(hojeIso(new Date(2026, 9, 20, 23, 59, 59))).toBe('2026-10-20')
    expect(hojeIso(new Date(2026, 9, 20, 0, 0, 1))).toBe('2026-10-20')
  })
})

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

describe('formatarDataHoraUtc', () => {
  // O fuso do ambiente de teste varia: a expectativa é calculada com a mesma conversão local.
  function esperado(utc: Date): string {
    const dois = (n: number) => String(n).padStart(2, '0')
    return (
      `${dois(utc.getDate())}/${dois(utc.getMonth() + 1)}/${utc.getFullYear()} ` +
      `${dois(utc.getHours())}:${dois(utc.getMinutes())}`
    )
  }
  const instante = new Date(Date.UTC(2026, 9, 20, 15, 30, 12))

  it('formata como dd/MM/yyyy HH:mm no fuso do navegador', () => {
    expect(formatarDataHoraUtc('2026-10-20T15:30:12Z')).toBe(esperado(instante))
  })

  it('texto sem designador de fuso é tratado como UTC (DateTime lido do banco, sem o "Z")', () => {
    expect(formatarDataHoraUtc('2026-10-20T15:30:12')).toBe(esperado(instante))
    expect(formatarDataHoraUtc('2026-10-20T15:30:12')).toBe(formatarDataHoraUtc('2026-10-20T15:30:12Z'))
  })

  it('aceita frações de segundo longas, como as do .NET (7 dígitos)', () => {
    expect(formatarDataHoraUtc('2026-10-20T15:30:12.1234567')).toBe(esperado(instante))
    expect(formatarDataHoraUtc('2026-10-20T15:30:12.1234567Z')).toBe(esperado(instante))
  })

  it('respeita um deslocamento explícito', () => {
    expect(formatarDataHoraUtc('2026-10-20T12:30:12-03:00')).toBe(esperado(instante))
  })

  it('devolve o texto original quando a data é inválida', () => {
    expect(formatarDataHoraUtc('isso não é uma data')).toBe('isso não é uma data')
  })
})
