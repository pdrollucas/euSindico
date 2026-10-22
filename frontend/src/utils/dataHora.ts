// DataCompromisso/HorarioCompromisso chegam como "2026-10-20" / "14:30:00" — horário de parede,
// sem fuso. Formatação feita sobre a string (nunca via `new Date(iso)`, que interpretaria a data
// como UTC e poderia mostrar o dia anterior).

// "20/10" no ano corrente de `hoje`; "20/10/2027" nos demais (concluídos de anos passados).
export function formatarData(dataIso: string, hoje: Date = new Date()): string {
  const [ano, mes, dia] = dataIso.split('-')
  const base = `${dia}/${mes}`
  return Number(ano) === hoje.getFullYear() ? base : `${base}/${ano}`
}

// Máscara do campo de horário digitado: só dígitos (no máximo 4), com ":" inserido sozinho depois
// do segundo — "1830" -> "18:30". O ":" só aparece quando há o terceiro dígito, para o backspace
// não "travar" num "18:".
export function mascararHorario(texto: string | null | undefined): string {
  const digitos = (texto ?? '').replace(/\D/g, '').slice(0, 4)
  return digitos.length > 2 ? `${digitos.slice(0, 2)}:${digitos.slice(2)}` : digitos
}

// "14:30:00" -> "14:30".
export function formatarHorario(horarioIso: string): string {
  return horarioIso.slice(0, 5)
}

// Instante gerado pelo servidor (ex: `GeradoEm` do relatório) -> "20/10/2026 12:30", no fuso do
// navegador. O backend grava em UTC, mas o EF devolve `DateTime` lido do banco sem o "Z" (e o recém
// criado, com): sem designador de fuso o texto é tratado como UTC, senão o JS o leria como hora
// local e mostraria um horário errado.
export function formatarDataHoraUtc(iso: string): string {
  const temFuso = /(Z|[+-]\d{2}:?\d{2})$/i.test(iso)
  const data = new Date(temFuso ? iso : `${iso}Z`)
  if (Number.isNaN(data.getTime())) {
    return iso
  }
  const dois = (n: number) => String(n).padStart(2, '0')
  return (
    `${dois(data.getDate())}/${dois(data.getMonth() + 1)}/${data.getFullYear()} ` +
    `${dois(data.getHours())}:${dois(data.getMinutes())}`
  )
}

// "2026-10-20" do dia de hoje no fuso do navegador — mesmo formato do <input type="date">, então
// strings ISO se comparam lexicograficamente (usado pelo `min` do campo de data e pela regra
// "não pode estar no passado" do planejamento, RN30). O backend continua sendo a fonte da verdade.
export function hojeIso(hoje: Date = new Date()): string {
  const mes = String(hoje.getMonth() + 1).padStart(2, '0')
  const dia = String(hoje.getDate()).padStart(2, '0')
  return `${hoje.getFullYear()}-${mes}-${dia}`
}
