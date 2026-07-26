// Réplica da regra já implementada no backend (NomeValidator) — ver
// frontend/documentation/SECURITY.md, seção 4: só letras (com acentuação), espaços, hífen e apóstrofo.
const NOME_VALIDO_REGEX = /^[\p{L}\s'-]+$/u

export function nomeEhValido(nome: string): boolean {
  return NOME_VALIDO_REGEX.test(nome)
}
