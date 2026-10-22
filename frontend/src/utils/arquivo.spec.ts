import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { abrirAbaEmBranco, baixarArquivo, mostrarArquivoNaAba } from './arquivo'

describe('utils/arquivo', () => {
  const criarUrl = vi.fn<(blob: Blob) => string>(() => 'blob:http://localhost/abc')
  const revogarUrl = vi.fn<(url: string) => void>()
  const blob = new Blob(['%PDF'], { type: 'application/pdf' })

  beforeEach(() => {
    vi.useFakeTimers()
    // O jsdom não implementa createObjectURL/revokeObjectURL.
    Object.defineProperty(URL, 'createObjectURL', { value: criarUrl, configurable: true })
    Object.defineProperty(URL, 'revokeObjectURL', { value: revogarUrl, configurable: true })
    criarUrl.mockClear()
    revogarUrl.mockClear()
  })

  afterEach(() => {
    vi.useRealTimers()
    vi.restoreAllMocks()
  })

  it('baixarArquivo clica num link com o nome do arquivo e o endereço do blob', () => {
    const clique = vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined)

    baixarArquivo(blob, 'Edificio-Aurora_Relatorio_2026-09.pdf')

    // `contexts` guarda o `this` de cada chamada: o próprio <a> que foi clicado.
    const link = clique.mock.contexts[0] as HTMLAnchorElement
    expect(clique).toHaveBeenCalledTimes(1)
    expect(link.download).toBe('Edificio-Aurora_Relatorio_2026-09.pdf')
    expect(link.href).toBe('blob:http://localhost/abc')
    // O link não fica pendurado na página.
    expect(document.querySelector('a[download]')).toBeNull()
  })

  it('libera o endereço temporário do blob depois da validade', () => {
    vi.spyOn(HTMLAnchorElement.prototype, 'click').mockImplementation(() => undefined)

    baixarArquivo(blob, 'x.pdf')
    expect(revogarUrl).not.toHaveBeenCalled()

    vi.advanceTimersByTime(60_000)

    expect(revogarUrl).toHaveBeenCalledWith('blob:http://localhost/abc')
  })

  it('abrirAbaEmBranco abre uma aba vazia em _blank', () => {
    const aba = {} as Window
    const abrir = vi.spyOn(window, 'open').mockReturnValue(aba)

    expect(abrirAbaEmBranco()).toBe(aba)
    expect(abrir).toHaveBeenCalledWith('', '_blank')
  })

  it('abrirAbaEmBranco devolve null quando o navegador bloqueia o pop-up', () => {
    vi.spyOn(window, 'open').mockReturnValue(null)

    expect(abrirAbaEmBranco()).toBeNull()
  })

  it('mostrarArquivoNaAba aponta a aba aberta para o endereço do blob', () => {
    const aba = { location: { href: '' } } as unknown as Window

    mostrarArquivoNaAba(aba, blob)

    expect(aba.location.href).toBe('blob:http://localhost/abc')
  })
})
