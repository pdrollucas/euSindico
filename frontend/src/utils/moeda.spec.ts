import { describe, it, expect } from 'vitest'
import {
  formatarMoeda,
  mascararMoeda,
  mascararMoedaDigitada,
  moedaParaNumero,
  numeroParaMoedaMascarada,
} from './moeda'

describe('formatarMoeda', () => {
  it('formata em reais com separador de milhar e vírgula decimal', () => {
    expect(formatarMoeda(15000.5)).toBe('R$ 15.000,50')
  })

  it('formata zero', () => {
    expect(formatarMoeda(0)).toBe('R$ 0,00')
  })

  it('usa espaço comum entre o símbolo e o valor', () => {
    expect(formatarMoeda(1)).not.toContain(' ')
  })

  it('formata centavos', () => {
    expect(formatarMoeda(0.01)).toBe('R$ 0,01')
  })
})

describe('mascararMoeda', () => {
  it.each([
    ['1', '0,01'],
    ['15', '0,15'],
    ['150', '1,50'],
    ['150050', '1.500,50'],
    ['99999999 99', '99.999.999,99'],
  ])('lê os dígitos de %j como centavos: %j', (entrada, esperado) => {
    expect(mascararMoeda(entrada)).toBe(esperado)
  })

  it('zero vira 0,00 e zeros à esquerda não se acumulam', () => {
    expect(mascararMoeda('0')).toBe('0,00')
    expect(mascararMoeda('0005')).toBe('0,05')
  })

  it('descarta letras e símbolos', () => {
    expect(mascararMoeda('R$ 1a5b0')).toBe('1,50')
  })

  it('limita a 10 dígitos (o teto do orçamento)', () => {
    expect(mascararMoeda('123456789012')).toBe('12.345.678,90')
  })

  it('devolve vazio sem dígitos', () => {
    expect(mascararMoeda('')).toBe('')
    expect(mascararMoeda(null)).toBe('')
    expect(mascararMoeda('abc')).toBe('')
  })

  it('é idempotente sobre um valor já mascarado', () => {
    expect(mascararMoeda('1.500,50')).toBe('1.500,50')
  })
})

describe('mascararMoedaDigitada', () => {
  it('digitar mascara normalmente', () => {
    expect(mascararMoedaDigitada('1', '')).toBe('0,01')
    expect(mascararMoedaDigitada('0,015', '0,01')).toBe('0,15')
  })

  it('zero digitado sozinho vira 0,00 (zero é um valor válido)', () => {
    expect(mascararMoedaDigitada('0', '')).toBe('0,00')
  })

  it('apagar até zerar esvazia o campo, em vez de travar em 0,00', () => {
    expect(mascararMoedaDigitada('0,0', '0,01')).toBe('')
  })

  it('apagar um dígito de um valor não nulo mantém o valor mascarado', () => {
    expect(mascararMoedaDigitada('1,5', '1,50')).toBe('0,15')
  })
})

describe('moedaParaNumero', () => {
  it('converte o valor mascarado em número', () => {
    expect(moedaParaNumero('1.500,50')).toBe(1500.5)
    expect(moedaParaNumero('0,01')).toBe(0.01)
    expect(moedaParaNumero('99.999.999,99')).toBe(99999999.99)
  })

  it('zero é número, não ausência de valor', () => {
    expect(moedaParaNumero('0,00')).toBe(0)
  })

  it('vazio é null', () => {
    expect(moedaParaNumero('')).toBeNull()
    expect(moedaParaNumero('  ')).toBeNull()
    expect(moedaParaNumero(null)).toBeNull()
    expect(moedaParaNumero(undefined)).toBeNull()
  })
})

describe('numeroParaMoedaMascarada', () => {
  it('prepara o valor vindo do backend para o campo', () => {
    expect(numeroParaMoedaMascarada(1500.5)).toBe('1.500,50')
    expect(numeroParaMoedaMascarada(0)).toBe('0,00')
    expect(numeroParaMoedaMascarada(0.1)).toBe('0,10')
  })

  it('ida e volta com moedaParaNumero preserva o valor', () => {
    expect(moedaParaNumero(numeroParaMoedaMascarada(15000.55))).toBe(15000.55)
  })
})
