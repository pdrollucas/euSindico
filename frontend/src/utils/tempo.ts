// Funções puras de contagem regressiva, usadas no cooldown de reenvio do código de redefinição
// de senha (RF06-A). Testadas em tempo.spec.ts. A parte reativa (setInterval) fica no composable
// useContagemRegressiva; aqui só o cálculo e a formatação, sem estado nem efeitos.

/** Segundos restantes até `expiraEmMs` (arredondados pra cima), nunca negativo; 0 se `null`. */
export function segundosRestantes(expiraEmMs: number | null, agoraMs: number = Date.now()): number {
  if (expiraEmMs === null) return 0
  const restante = Math.ceil((expiraEmMs - agoraMs) / 1000)
  return restante > 0 ? restante : 0
}

/** Formata segundos como `m:ss` (ex: 65 -> "1:05", 300 -> "5:00"). */
export function formatarContagem(segundos: number): string {
  const total = Math.max(0, Math.floor(segundos))
  const minutos = Math.floor(total / 60)
  const resto = total % 60
  return `${minutos}:${resto.toString().padStart(2, '0')}`
}
