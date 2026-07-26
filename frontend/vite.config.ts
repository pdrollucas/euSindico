import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'
import vuetify from 'vite-plugin-vuetify'
import istanbul from 'vite-plugin-istanbul'

// https://vite.dev/config/
export default defineConfig({
  plugins: [
    vue(),
    // Painel do Vue DevTools cobre elementos da tela e causa cy.type()/cy.click() intermitente.
    ...(process.env.CYPRESS ? [] : [vueDevTools()]),
    vuetify({ autoImport: true }),
    // Fora do Vitest: ele herda os plugins daqui via mergeConfig, e istanbul conflita com o
    // provider v8 do Vitest (relatório de cobertura ficava incompleto).
    ...(process.env.VITEST
      ? []
      : [
          istanbul({
            include: 'src/*',
            exclude: ['node_modules', 'cypress/', '**/*.spec.ts'],
            extension: ['.js', '.ts', '.vue'],
            requireEnv: false,
          }),
        ]),
  ],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url)),
    },
  },
  server: {
    // Evita reload no meio de um import() dinâmico durante o primeiro cy.visit().
    warmup: {
      clientFiles: ['./src/main.ts', './src/router/index.ts', './src/views/**/*.vue'],
    },
  },
})
