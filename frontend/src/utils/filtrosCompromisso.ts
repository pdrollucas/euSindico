import type { CompromissoFiltros } from '@/types/compromisso.types'
import { anosComFuturo, nomeMes, periodoAtual } from '@/utils/periodo'

// Compartilhado pela folha de filtros (celular) e pela barra (desktop).

export const ITENS_ANO_COMPROMISSO = anosComFuturo(2)

export const ITENS_MES = Array.from({ length: 12 }, (_, indice) => ({
  title: nomeMes(indice + 1),
  value: indice + 1,
}))

// Ano e mês andam juntos: escolher um sem o outro completa com o atual, e limpar um limpa os dois.
export function parcialAoEscolherAno(
  valor: number | null,
  filtros: Pick<CompromissoFiltros, 'mes'>,
): Partial<CompromissoFiltros> {
  return valor ? { ano: valor, mes: filtros.mes ?? periodoAtual().mes } : { ano: null, mes: null }
}

export function parcialAoEscolherMes(
  valor: number | null,
  filtros: Pick<CompromissoFiltros, 'ano'>,
): Partial<CompromissoFiltros> {
  return valor ? { mes: valor, ano: filtros.ano ?? periodoAtual().ano } : { ano: null, mes: null }
}
