const formatadorBrl = new Intl.NumberFormat('pt-BR', { style: 'currency', currency: 'BRL' })

// 10 dígitos = 99.999.999,99, o teto do orçamento (decimal(10,2) no backend).
const MAX_DIGITOS = 10

// 15000.5 -> "R$ 15.000,50". O Intl separa o símbolo do valor com um espaço não separável; troca
// por um espaço comum para o texto poder ser comparado e copiado sem surpresas.
export function formatarMoeda(valor: number): string {
  return formatadorBrl.format(valor).replace(/ /g, ' ')
}

// Máscara do campo de valor: só dígitos (no máximo 10), lidos como centavos — "5" -> "0,05",
// "150050" -> "1.500,50". A vírgula e os pontos aparecem sozinhos conforme o usuário digita.
export function mascararMoeda(texto: string | null | undefined): string {
  const digitos = (texto ?? '').replace(/\D/g, '').slice(0, MAX_DIGITOS)
  if (!digitos) {
    return ''
  }
  const centavos = String(Number(digitos)).padStart(3, '0')
  const inteiro = centavos.slice(0, -2).replace(/\B(?=(\d{3})+(?!\d))/g, '.')
  return `${inteiro},${centavos.slice(-2)}`
}

// Igual a `mascararMoeda`, mas deixa o campo ser esvaziado: ao apagar, "0,01" viraria "0,00" e o
// campo nunca ficaria vazio (nem sem valor). Zero continua digitável: "0" sozinho vira "0,00".
export function mascararMoedaDigitada(novo: string | null | undefined, anterior: string): string {
  const mascarado = mascararMoeda(novo)
  const apagando = (novo ?? '').length < anterior.length
  return apagando && moedaParaNumero(mascarado) === 0 ? '' : mascarado
}

// "1.500,50" -> 1500.5. Vazio -> null (sem valor informado).
export function moedaParaNumero(mascarado: string | null | undefined): number | null {
  const texto = (mascarado ?? '').trim()
  if (!texto) {
    return null
  }
  return Number(texto.replace(/\./g, '').replace(',', '.'))
}

// 1500.5 -> "1.500,50", para preencher o campo ao editar.
export function numeroParaMoedaMascarada(valor: number): string {
  return mascararMoeda(String(Math.round(valor * 100)))
}
