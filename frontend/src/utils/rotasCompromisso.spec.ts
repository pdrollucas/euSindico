import { describe, it, expect } from 'vitest'
import { lerPredioIdDaQuery, rotaListaCompromissos } from './rotasCompromisso'

describe('lerPredioIdDaQuery', () => {
  it('lê um id válido', () => {
    expect(lerPredioIdDaQuery('7')).toBe(7)
  })

  it('usa o primeiro valor quando a query repete o parâmetro', () => {
    expect(lerPredioIdDaQuery(['3', '9'])).toBe(3)
  })

  it.each([undefined, '', 'abc', '0', '-2', '1.5'])('ignora o valor inválido %j', (valor) => {
    expect(lerPredioIdDaQuery(valor)).toBeNull()
  })
})

describe('rotaListaCompromissos', () => {
  it('volta para a lista geral sem prédio', () => {
    expect(rotaListaCompromissos(undefined)).toBe('/compromissos')
  })

  it('preserva o filtro de prédio', () => {
    expect(rotaListaCompromissos('4')).toEqual({ path: '/compromissos', query: { predioId: '4' } })
  })
})
