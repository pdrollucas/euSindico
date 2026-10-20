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
