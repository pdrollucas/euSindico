import { createApp } from 'vue'
import { createPinia } from 'pinia'

import App from './App.vue'
import router from './router'
import vuetify from './plugins/vuetify'

const app = createApp(App)

app.use(createPinia())
app.use(vuetify)
app.use(router)

// Bootstrap da sessão (POST /auth/refresh) não acontece aqui — só na guarda de rota
// (router/index.ts), e só na primeira vez que uma rota protegida é visitada. Rodar isso
// incondicionalmente para toda rota (inclusive a landing pública) gastaria uma chamada de rede
// à toa em telas que não precisam de sessão nenhuma — ver AUTHENTICATION.md, seção 4.
app.mount('#app')
