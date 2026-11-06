// Breakpoint `md` do Vuetify (useDisplay().mdAndUp), o mesmo das media queries de main.css.
export const MEDIA_DESKTOP = '(min-width: 840px)'

// Para fora de componentes (guarda do router), onde o useDisplay não está disponível.
export function ehDesktop(): boolean {
  return typeof window.matchMedia === 'function' && window.matchMedia(MEDIA_DESKTOP).matches
}
