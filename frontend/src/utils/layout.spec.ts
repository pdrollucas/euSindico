import { afterEach, describe, expect, it, vi } from 'vitest'
import { ehDesktop, MEDIA_DESKTOP } from './layout'

describe('ehDesktop', () => {
  const original = window.matchMedia

  afterEach(() => {
    window.matchMedia = original
  })

  it.each([true, false])('reflete a media query de desktop (matches = %s)', (matches) => {
    const matchMedia = vi.fn<(query: string) => MediaQueryList>(
      () => ({ matches }) as MediaQueryList,
    )
    window.matchMedia = matchMedia

    expect(ehDesktop()).toBe(matches)
    expect(matchMedia).toHaveBeenCalledWith(MEDIA_DESKTOP)
  })

  it('assume celular quando o navegador não tem matchMedia', () => {
    // @ts-expect-error simulando ambiente sem a API (ex: jsdom)
    window.matchMedia = undefined

    expect(ehDesktop()).toBe(false)
  })
})
