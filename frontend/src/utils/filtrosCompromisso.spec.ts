import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest'
import { ITENS_MES, parcialAoEscolherAno, parcialAoEscolherMes } from './filtrosCompromisso'

describe('filtrosCompromisso', () => {
  beforeEach(() => {
    vi.useFakeTimers()
    vi.setSystemTime(new Date(2026, 10, 5)) // novembro de 2026
  })

  afterEach(() => {
    vi.useRealTimers()
  })

  it('lista os 12 meses com nome', () => {
    expect(ITENS_MES).toHaveLength(12)
    expect(ITENS_MES[0]).toEqual({ title: 'Janeiro', value: 1 })
  })

  it('escolher o ano mantém o mês já escolhido', () => {
    expect(parcialAoEscolherAno(2025, { mes: 3 })).toEqual({ ano: 2025, mes: 3 })
  })

  it('escolher o ano sem mês completa com o mês atual', () => {
    expect(parcialAoEscolherAno(2025, { mes: null })).toEqual({ ano: 2025, mes: 11 })
  })

  it('escolher o mês sem ano completa com o ano atual', () => {
    expect(parcialAoEscolherMes(4, { ano: null })).toEqual({ mes: 4, ano: 2026 })
  })

  it('limpar o ano ou o mês limpa o período inteiro', () => {
    expect(parcialAoEscolherAno(null, { mes: 3 })).toEqual({ ano: null, mes: null })
    expect(parcialAoEscolherMes(null, { ano: 2025 })).toEqual({ ano: null, mes: null })
  })
})
