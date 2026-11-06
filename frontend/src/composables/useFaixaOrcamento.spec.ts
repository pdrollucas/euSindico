import { describe, expect, it, vi } from 'vitest'
import { nextTick, reactive } from 'vue'
import type { PlanejamentoFiltros } from '@/types/planejamento.types'
import { useFaixaOrcamento } from './useFaixaOrcamento'

function montar(inicial: Partial<PlanejamentoFiltros> = {}) {
  const filtros = reactive<PlanejamentoFiltros>({
    titulo: '',
    orcamentoMinimo: null,
    orcamentoMaximo: null,
    ...inicial,
  })
  const alterar = vi.fn<(parcial: Partial<PlanejamentoFiltros>) => void>()
  return { filtros, alterar, faixa: useFaixaOrcamento(() => filtros, alterar) }
}

describe('useFaixaOrcamento', () => {
  it('preenche os campos com os filtros atuais', () => {
    const { faixa } = montar({ orcamentoMinimo: 1500, orcamentoMaximo: null })

    expect(faixa.minimo.value).toBe('1.500,00')
    expect(faixa.maximo.value).toBe('')
  })

  it('aplica só os limites que mudaram', () => {
    const { faixa, alterar } = montar({ orcamentoMaximo: 900 })
    faixa.minimo.value = '100,00'

    expect(faixa.aplicar()).toBe(true)
    expect(alterar).toHaveBeenCalledWith({ orcamentoMinimo: 100 })
  })

  it('não emite nada quando nada mudou', () => {
    const { faixa, alterar } = montar()

    expect(faixa.aplicar()).toBe(true)
    expect(alterar).not.toHaveBeenCalled()
  })

  it('barra mínimo maior que o máximo', () => {
    const { faixa, alterar } = montar()
    faixa.minimo.value = '500,00'
    faixa.maximo.value = '100,00'

    expect(faixa.aplicar()).toBe(false)
    expect(faixa.erroMinimo.value).toBe('O mínimo não pode ser maior que o máximo')
    expect(alterar).not.toHaveBeenCalled()
  })

  it('acompanha mudanças feitas por fora (ex: "Limpar filtros")', async () => {
    const { faixa, filtros } = montar({ orcamentoMinimo: 100 })
    faixa.erroMinimo.value = 'erro antigo'

    filtros.orcamentoMinimo = null
    await nextTick()

    expect(faixa.minimo.value).toBe('')
    expect(faixa.erroMinimo.value).toBeNull()
  })
})
