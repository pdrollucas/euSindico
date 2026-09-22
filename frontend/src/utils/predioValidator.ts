// Réplica das regras do backend (PredioNomeValidator, EnderecoValidator) — allowlist, não
// "sanitização".
const PREDIO_NOME_REGEX = /^[\p{L}\p{N}\s.,'\-()/ºª]+$/u
const ENDERECO_REGEX = /^[\p{L}\p{N}\s.,'\-/ºª]+$/u

export function predioNomeEhValido(nome: string): boolean {
  return PREDIO_NOME_REGEX.test(nome)
}

export function enderecoEhValido(endereco: string): boolean {
  return ENDERECO_REGEX.test(endereco)
}
