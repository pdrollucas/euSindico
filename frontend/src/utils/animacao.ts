// Tempos da saída de um item da lista de compromissos (card no celular, tabela no desktop) — o
// CSS dos componentes e o `setTimeout` da view precisam concordar, por isso vivem num lugar só.
//
// Concluir: riscado do título (350 ms) -> pausa curta -> fade (300 ms) = 750 ms.
// Reabrir (na lista de concluídos): só o fade, sem riscado.
export const SAIDA_CONCLUIR_MS = 750
export const SAIDA_REABRIR_MS = 300

// Quem pede menos movimento (prefers-reduced-motion) não espera animação nenhuma.
export function reduzirMovimento(): boolean {
  return typeof window !== 'undefined' && window.matchMedia?.('(prefers-reduced-motion: reduce)').matches === true
}
