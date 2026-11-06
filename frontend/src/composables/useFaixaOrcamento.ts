import { nextTick, ref, watch } from 'vue'
import type { PlanejamentoFiltros } from '@/types/planejamento.types'
import { mascararMoedaDigitada, moedaParaNumero, numeroParaMoedaMascarada } from '@/utils/moeda'

// Faixa de orçamento dos planejamentos, compartilhada pela folha (celular) e pela barra (desktop).
// Os campos são digitados: aplicam ao sair do campo, no Enter ou ao fechar a folha — não a cada
// tecla, para não gerar uma requisição por dígito.
export function useFaixaOrcamento(
  filtros: () => PlanejamentoFiltros,
  alterar: (parcial: Partial<PlanejamentoFiltros>) => void,
) {
  const paraCampo = (valor: number | null) =>
    valor === null ? '' : numeroParaMoedaMascarada(valor)

  const minimo = ref(paraCampo(filtros().orcamentoMinimo))
  const maximo = ref(paraCampo(filtros().orcamentoMaximo))
  const erroMinimo = ref<string | null>(null)

  // "Limpar filtros" e os chips mudam os filtros por fora — os campos acompanham.
  watch(
    filtros,
    (atuais) => {
      minimo.value = paraCampo(atuais.orcamentoMinimo)
      maximo.value = paraCampo(atuais.orcamentoMaximo)
      erroMinimo.value = null
    },
    { deep: true },
  )

  // Mesma máscara do formulário (ver PlanejamentoForm.vue): o valor mascarado volta ao próprio
  // <input>, senão uma letra digitada ficaria visível quando o texto mascarado não muda.
  const minimoCampo = ref<{ $el: HTMLElement } | null>(null)
  const maximoCampo = ref<{ $el: HTMLElement } | null>(null)

  function digitado(valor: string | null, limite: 'minimo' | 'maximo') {
    const alvo = limite === 'minimo' ? minimo : maximo
    const campo = limite === 'minimo' ? minimoCampo : maximoCampo
    const mascarado = mascararMoedaDigitada(valor, alvo.value)
    alvo.value = mascarado
    nextTick(() => {
      const input = campo.value?.$el.querySelector('input')
      if (input && input.value !== mascarado) {
        input.value = mascarado
      }
    })
  }

  // Devolve false quando a faixa é inválida (mínimo maior que o máximo).
  function aplicar(): boolean {
    const novoMinimo = moedaParaNumero(minimo.value)
    const novoMaximo = moedaParaNumero(maximo.value)

    erroMinimo.value = null
    if (novoMinimo !== null && novoMaximo !== null && novoMinimo > novoMaximo) {
      erroMinimo.value = 'O mínimo não pode ser maior que o máximo'
      return false
    }

    const atuais = filtros()
    const parcial: Partial<PlanejamentoFiltros> = {}
    if (novoMinimo !== atuais.orcamentoMinimo) {
      parcial.orcamentoMinimo = novoMinimo
    }
    if (novoMaximo !== atuais.orcamentoMaximo) {
      parcial.orcamentoMaximo = novoMaximo
    }
    if (Object.keys(parcial).length > 0) {
      alterar(parcial)
    }
    return true
  }

  return { minimo, maximo, erroMinimo, minimoCampo, maximoCampo, digitado, aplicar }
}
