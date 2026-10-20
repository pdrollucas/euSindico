// Réplica das regras do backend (TituloValidator, PrestadorServicoValidator, DetalhesValidator) —
// allowlist, não "sanitização". O escape do Vue continua sendo a defesa primária contra XSS.
const TITULO_REGEX = /^[\p{L}\p{N}\s.,:;'\-()/ºª&%#!?+@$°]+$/u

export function tituloEhValido(titulo: string): boolean {
  return TITULO_REGEX.test(titulo)
}

// Mesma allowlist do título; só é checado quando preenchido.
export function prestadorEhValido(nome: string): boolean {
  return TITULO_REGEX.test(nome)
}

// Texto livre de propósito ("R$ 1.200,00", "e-mail@x.com", quebras de linha) — o backend só
// rejeita "<" e ">".
export function detalhesEhValido(detalhes: string): boolean {
  return !detalhes.includes('<') && !detalhes.includes('>')
}
