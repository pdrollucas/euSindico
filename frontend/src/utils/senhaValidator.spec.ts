import { describe, it, expect } from 'vitest'
import { senhaEhForte } from './senhaValidator'

describe('senhaEhForte', () => {
  it('rejeita senha sem caractere especial', () => {
    expect(senhaEhForte('Abc12345')).toBe(false)
  })

  it('aceita senha que atende RNF04', () => {
    expect(senhaEhForte('Abc12345!')).toBe(true)
  })

  it('rejeita senha com menos de 8 caracteres', () => {
    expect(senhaEhForte('Ab1!')).toBe(false)
  })

  it('rejeita senha sem letra maiúscula', () => {
    expect(senhaEhForte('abc12345!')).toBe(false)
  })

  it('rejeita senha sem número', () => {
    expect(senhaEhForte('Abcdefgh!')).toBe(false)
  })
})
