// Réplica das regras já implementadas no backend (SenhaForteValidator, RNF04) — ver
// frontend/documentation/SECURITY.md, seção 4. Fonte única usada tanto pelo schema Zod
// (schemas/auth.schema.ts) quanto por qualquer validação avulsa.
const SENHA_FORTE_REGEX = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/

export function senhaEhForte(senha: string): boolean {
  return SENHA_FORTE_REGEX.test(senha)
}
