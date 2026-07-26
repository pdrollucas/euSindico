import { mount } from '@cypress/vue'
import { createPinia } from 'pinia'
import { createAppVuetify } from '../../src/plugins/vuetify'
import './commands'

// Coleta a cobertura istanbul (window.__coverage__) instrumentada pelo vite-plugin-istanbul
// ao final de cada teste — alimenta cypress-coverage/lcov.info (ver documentation/TEST.md, seção 7).
import '@cypress/code-coverage/support'

type MountParams = Parameters<typeof mount>

// Toda montagem de componente já sobe com Vuetify + Pinia registrados — ver
// frontend/documentation/TEST.md, seção 4. Instância nova de Vuetify (e de Pinia) a cada
// mount — cada `cy.mount` sobe um app Vue novo, e reaproveitar a mesma instância de Vuetify
// entre apps diferentes quebra em runtime (ver comentário em src/plugins/vuetify.ts).
Cypress.Commands.add('mount', (component: MountParams[0], options: MountParams[1] = {}) => {
  options.global = options.global ?? {}
  options.global.plugins = options.global.plugins ?? []
  options.global.plugins.push(createAppVuetify(), createPinia())

  return mount(component, options)
})

declare global {
  // eslint-disable-next-line @typescript-eslint/no-namespace
  namespace Cypress {
    interface Chainable {
      mount: typeof mount
    }
  }
}
