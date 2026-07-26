import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'

import { createVuetify } from 'vuetify'

// Fábrica, não só um singleton: uma instância de createVuetify() é interna ao app Vue em que
// foi registrada (app.use()) — reaproveitá-la entre apps diferentes (ex: um novo app por teste
// em cy.mount, ver cypress/support/component.ts) quebra em runtime ("Cannot read properties of
// undefined (reading 'app')"). main.ts chama isso uma única vez; testes de componente chamam de
// novo a cada mount. Tema e breakpoints mobile-first (RFC, RNF08) ficam centralizados aqui — ver
// frontend/documentation/ARCHITECTURE.md, seção 1.
export function createAppVuetify() {
  return createVuetify({
    icons: {
      defaultSet: 'mdi',
    },
  })
}

export default createAppVuetify()
