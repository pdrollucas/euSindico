import type { RouteLocationRaw } from 'vue-router'

// `?predioId=` é como a tela do prédio abre a lista de compromissos já filtrada (a rota é uma só,
// /compromissos — ver COMPROMISSOS.md). O valor também acompanha as telas filhas (novo/detalhe)
// para o "voltar" retornar à mesma lista. Valor inválido na URL é ignorado.
export function lerPredioIdDaQuery(valor: unknown): number | null {
  const texto = Array.isArray(valor) ? valor[0] : valor
  const numero = Number(texto)
  return Number.isInteger(numero) && numero > 0 ? numero : null
}

export function rotaListaCompromissos(predioId?: unknown): RouteLocationRaw {
  const id = lerPredioIdDaQuery(predioId)
  return id ? { path: '/compromissos', query: { predioId: String(id) } } : '/compromissos'
}
