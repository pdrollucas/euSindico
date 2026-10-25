import type { SituacaoCompromissos } from '@/types/compromisso.types'

// Mês/ano de referência dos relatórios (RN34). "Hoje" é o do navegador; o backend valida o mês
// futuro no fuso America/Sao_Paulo e é a fonte da verdade — aqui só evita um 400 previsível.

// Mesmo piso da validação do backend (GerarRelatorioDtoValidator.AnoMinimo).
export const ANO_MINIMO = 2000

export interface Periodo {
  mes: number
  ano: number
}

export interface OpcaoMes {
  title: string
  value: number
  props: { disabled: boolean }
}

const formatadorMes = new Intl.DateTimeFormat('pt-BR', { month: 'long' })

// 9 -> "Setembro".
export function nomeMes(mes: number): string {
  const nome = formatadorMes.format(new Date(2000, mes - 1, 1))
  return nome.charAt(0).toUpperCase() + nome.slice(1)
}

// (9, 2026) -> "Setembro de 2026".
export function formatarPeriodo(mes: number, ano: number): string {
  return `${nomeMes(mes)} de ${ano}`
}

export function periodoAtual(hoje: Date = new Date()): Periodo {
  return { mes: hoje.getMonth() + 1, ano: hoje.getFullYear() }
}

// O mês corrente é permitido (gera um relatório parcial); só o que ainda não começou é futuro.
export function periodoEhFuturo(mes: number, ano: number, hoje: Date = new Date()): boolean {
  const atual = periodoAtual(hoje)
  return ano > atual.ano || (ano === atual.ano && mes > atual.mes)
}

// Do ano corrente para trás, até o piso: o mais recente primeiro, que é o mais provável.
export function anosDisponiveis(hoje: Date = new Date()): number[] {
  const anos: number[] = []
  for (let ano = hoje.getFullYear(); ano >= ANO_MINIMO; ano--) {
    anos.push(ano)
  }
  return anos
}

// Como `anosDisponiveis`, mas começando `adiante` anos no futuro: compromissos podem ser agendados
// para depois, ao contrário dos relatórios.
export function anosComFuturo(adiante: number, hoje: Date = new Date()): number[] {
  const anos: number[] = []
  for (let ano = hoje.getFullYear() + adiante; ano >= ANO_MINIMO; ano--) {
    anos.push(ano)
  }
  return anos
}

// Os 12 meses do ano escolhido, com os futuros desabilitados.
export function mesesDoAno(ano: number, hoje: Date = new Date()): OpcaoMes[] {
  return Array.from({ length: 12 }, (_, indice) => {
    const mes = indice + 1
    return { title: nomeMes(mes), value: mes, props: { disabled: periodoEhFuturo(mes, ano, hoje) } }
  })
}

// Situações em que o período é sempre um mês (a lista cresce sem limite com o tempo de uso).
// "Hoje" não usa período; "Atrasados" o aceita, mas parte sem ele.
export function periodoObrigatorio(situacao: SituacaoCompromissos): boolean {
  return situacao === 'proximos' || situacao === 'concluidos' || situacao === 'todos'
}

// Período a usar ao trocar de situação: Hoje e Atrasados partem sem período; nas demais mantém o
// que o usuário já escolheu, ou cai no mês atual.
export function periodoAoTrocarSituacao(
  situacao: SituacaoCompromissos,
  atual: { ano: number | null; mes: number | null },
  hoje: Date = new Date(),
): { ano: number | null; mes: number | null } {
  if (!periodoObrigatorio(situacao)) {
    return { ano: null, mes: null }
  }
  if (atual.ano !== null && atual.mes !== null) {
    return { ano: atual.ano, mes: atual.mes }
  }
  const { ano, mes } = periodoAtual(hoje)
  return { ano, mes }
}
