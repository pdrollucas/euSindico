import 'vuetify/styles'
import '@mdi/font/css/materialdesignicons.css'

import { createVuetify } from 'vuetify'

const euSindicoTheme = {
  dark: false,
  colors: {
    primary: '#1E4FA8',
    secondary: '#74A8FF',
    background: '#F6F9FE',
    surface: '#FFFFFF',
    'on-primary': '#FFFFFF',
    'on-background': '#1B2536',
    'on-surface': '#1B2536',
  },
}

export function createAppVuetify() {
  return createVuetify({
    theme: {
      defaultTheme: 'euSindico',
      themes: {
        euSindico: euSindicoTheme,
      },
    },
    icons: {
      defaultSet: 'mdi',
    },
  })
}

export default createAppVuetify()
