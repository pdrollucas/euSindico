import { ref, watch, onScopeDispose, type Ref } from 'vue'
import { segundosRestantes } from '@/utils/tempo'

// Contagem regressiva reativa a partir de um timestamp de expiração em ms (ex: o cooldown de
// reenvio de código, ver stores/recuperacaoSenhaStore.ts). Expõe `segundos`, atualizado a cada
// 1s enquanto houver tempo restante; para sozinha ao zerar, ao `expiraEm` mudar e ao escopo do
// componente ser destruído. O cálculo puro vive em utils/tempo.ts.
export function useContagemRegressiva(expiraEm: Ref<number | null>) {
  const segundos = ref(segundosRestantes(expiraEm.value))
  let intervalo: ReturnType<typeof setInterval> | null = null

  function parar() {
    if (intervalo !== null) {
      clearInterval(intervalo)
      intervalo = null
    }
  }

  function atualizar() {
    segundos.value = segundosRestantes(expiraEm.value)
    if (segundos.value === 0) parar()
  }

  function iniciar() {
    parar()
    atualizar()
    if (segundos.value > 0) {
      intervalo = setInterval(atualizar, 1000)
    }
  }

  watch(expiraEm, iniciar, { immediate: true })
  onScopeDispose(parar)

  return { segundos }
}
