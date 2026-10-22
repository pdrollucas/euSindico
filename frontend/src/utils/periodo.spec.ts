import { describe, it, expect } from 'vitest'
import {
  ANO_MINIMO,
  anosDisponiveis,
  formatarPeriodo,
  mesesDoAno,
  nomeMes,
  periodoAtual,
  periodoEhFuturo,
} from './periodo'

// 20/10/2026, horário local.
const hoje = new Date(2026, 9, 20, 12, 0, 0)

describe('nomeMes / formatarPeriodo', () => {
  it('devolve o nome do mês em português com a inicial maiúscula', () => {
    expect(nomeMes(1)).toBe('Janeiro')
    expect(nomeMes(3)).toBe('Março')
    expect(nomeMes(12)).toBe('Dezembro')
  })

  it('formata o período como "Mês de ano"', () => {
    expect(formatarPeriodo(9, 2026)).toBe('Setembro de 2026')
  })
})

describe('periodoAtual', () => {
  it('usa o mês (1 a 12) e o ano da data informada', () => {
    expect(periodoAtual(hoje)).toEqual({ mes: 10, ano: 2026 })
  })
})

describe('periodoEhFuturo', () => {
  it('o mês corrente não é futuro (gera relatório parcial)', () => {
    expect(periodoEhFuturo(10, 2026, hoje)).toBe(false)
  })

  it('meses anteriores não são futuros', () => {
    expect(periodoEhFuturo(9, 2026, hoje)).toBe(false)
    expect(periodoEhFuturo(12, 2025, hoje)).toBe(false)
  })

  it('o mês seguinte e anos seguintes são futuros', () => {
    expect(periodoEhFuturo(11, 2026, hoje)).toBe(true)
    expect(periodoEhFuturo(1, 2027, hoje)).toBe(true)
  })
})

describe('anosDisponiveis', () => {
  it('vai do ano corrente até o piso, do mais recente para o mais antigo', () => {
    const anos = anosDisponiveis(hoje)

    expect(anos[0]).toBe(2026)
    expect(anos.at(-1)).toBe(ANO_MINIMO)
    expect(anos).toHaveLength(2026 - ANO_MINIMO + 1)
  })
})

describe('mesesDoAno', () => {
  it('lista os 12 meses', () => {
    const meses = mesesDoAno(2025, hoje)

    expect(meses).toHaveLength(12)
    expect(meses[0]).toMatchObject({ title: 'Janeiro', value: 1 })
    expect(meses[11]).toMatchObject({ title: 'Dezembro', value: 12 })
  })

  it('num ano passado nenhum mês fica desabilitado', () => {
    expect(mesesDoAno(2025, hoje).some((m) => m.props.disabled)).toBe(false)
  })

  it('no ano corrente desabilita só os meses depois do atual', () => {
    const desabilitados = mesesDoAno(2026, hoje)
      .filter((m) => m.props.disabled)
      .map((m) => m.value)

    expect(desabilitados).toEqual([11, 12])
  })
})
