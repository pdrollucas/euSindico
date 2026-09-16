import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'cypress'
import codeCoverageTask from '@cypress/code-coverage/task'
import vue from '@vitejs/plugin-vue'
import vuetify from 'vite-plugin-vuetify'
import istanbul from 'vite-plugin-istanbul'

export default defineConfig({
  // Só em run mode (CI): o servidor Vite recém-subido às vezes ainda está compilando o chunk da
  // rota quando o primeiro cy.visit() chega (mesmo com o warmup do vite.config.ts, que roda em
  // background e não é aguardado pelo start-server-and-test), gerando "Failed to fetch
  // dynamically imported module". Retry resolve porque na 2ª tentativa o módulo já está pronto;
  // um erro real da aplicação falharia de novo. Fica de fora do openMode para não mascarar bugs
  // durante o desenvolvimento local.
  retries: {
    runMode: 2,
    openMode: 0,
  },
  e2e: {
    specPattern: 'cypress/e2e/**/*.{cy,spec}.{js,jsx,ts,tsx}',
    baseUrl: 'http://localhost:5173',
    setupNodeEvents(on, config) {
      return codeCoverageTask(on, config)
    },
  },
  component: {
    specPattern: 'cypress/component/**/*.cy.{js,jsx,ts,tsx}',
    setupNodeEvents(on, config) {
      return codeCoverageTask(on, config)
    },
    devServer: {
      framework: 'vue',
      bundler: 'vite',
      // Config própria (não reaproveita vite.config.ts) para deixar de fora o vueDevTools() —
      // ele mantém estado global preso a uma única instância de app Vue e quebra
      // ("Cannot read properties of undefined (reading 'app')") quando o Cypress desmonta e
      // monta um app novo a cada teste, algo que o dev server normal nunca faz.
      viteConfig: {
        plugins: [
          vue(),
          vuetify({ autoImport: true }),
          istanbul({
            include: 'src/*',
            exclude: ['node_modules', 'cypress/', '**/*.spec.ts'],
            extension: ['.js', '.ts', '.vue'],
            requireEnv: false,
          }),
        ],
        resolve: {
          alias: {
            '@': fileURLToPath(new URL('./src', import.meta.url)),
          },
        },
      },
    },
  },
})
