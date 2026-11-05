import { describe, it, expect, vi, beforeEach } from 'vitest'
import { flushPromises, mount } from '@vue/test-utils'
import QrCodeCelular from './QrCodeCelular.vue'

// Só a forma usada pelo componente (promise); o `toDataURL` real também tem sobrecargas com callback.
const { toDataURL } = vi.hoisted(() => ({
  toDataURL: vi.fn<(texto: string, opcoes?: object) => Promise<string>>(),
}))

vi.mock('qrcode', () => ({ default: { toDataURL } }))

describe('QrCodeCelular', () => {
  beforeEach(() => {
    toDataURL.mockReset()
  })

  it.each(['/login', '/registrar'] as const)('aponta para a própria tela (%s)', async (caminho) => {
    toDataURL.mockResolvedValue('data:image/png;base64,qr')

    const wrapper = mount(QrCodeCelular, { props: { caminho } })
    await flushPromises()

    expect(toDataURL).toHaveBeenCalledWith(
      `${window.location.origin}${caminho}`,
      expect.any(Object),
    )
    expect(wrapper.find('[data-cy=qrcode-celular] img').attributes('src')).toBe(
      'data:image/png;base64,qr',
    )
  })

  it('não renderiza nada quando a geração falha', async () => {
    toDataURL.mockRejectedValue(new Error('falhou'))

    const wrapper = mount(QrCodeCelular, { props: { caminho: '/login' } })
    await flushPromises()

    expect(wrapper.find('[data-cy=qrcode-celular]').exists()).toBe(false)
  })
})
