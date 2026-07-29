<script setup lang="ts">
import logoUrl from '@/assets/logo-eusindico.svg'
import logoIconUrl from '@/assets/logo-eusindico-icon.svg'

// Landing pública — primeira tela do produto (ver ARCHITECTURE.md, nota sobre landing page).
// Objetivo: apresentar o euSíndico ao síndico profissional e convencê-lo a criar conta / entrar.
const beneficios = [
  {
    icone: 'mdi-view-dashboard-outline',
    titulo: 'Tudo em um só lugar',
    texto:
      'Compromissos, prédios e documentos centralizados. Chega de alternar entre agenda, nuvem e anotações soltas.',
  },
  {
    icone: 'mdi-clipboard-check-outline',
    titulo: 'Só o que você precisa',
    texto:
      "Pensado para a rotina diária do síndico, com foco em compromissos e documentos, sem a complexidade dos sistemas tradicionais de administração condominial."
  },
  {
    icone: 'mdi-file-chart-outline',
    titulo: 'Relatórios automáticos',
    texto:
      'Relatórios mensais gerados a partir dos compromissos concluídos. Sem montar nada à mão.',
  },
  {
    icone: 'mdi-folder-multiple-outline',
    titulo: 'Documentos por prédio',
    texto: 'Atas e normas organizadas por condomínio, acessíveis em segundos quando você precisar.',
  },
  {
    icone: 'mdi-shield-check-outline',
    titulo: 'Seus dados protegidos',
    texto:
      'Acesso autenticado, senhas criptografadas e conformidade com a LGPD. Só você enxerga as informações dos seus condomínios.',
  },
]
</script>

<template>
  <div class="landing">
    <!-- Cabeçalho -->
    <header class="landing-header">
      <div class="landing-container d-flex align-center justify-space-between py-3">
        <div class="d-flex align-center ga-2">
          <img :src="logoIconUrl" alt="euSíndico" class="landing-logo-mark" />
          <span class="text-h6 font-weight-bold" style="color: rgb(var(--v-theme-primary))">
            euSíndico
          </span>
        </div>
        <v-btn variant="text" color="primary" to="/login" data-cy="btn-login">Entrar</v-btn>
      </div>
    </header>

    <!-- Hero -->
    <section class="landing-hero">
      <div class="landing-container text-center py-12">
        <img :src="logoUrl" alt="euSíndico" class="landing-logo-hero mb-6" />
        <h1 class="text-h4 text-md-h3 font-weight-bold mb-4">
          Organização para síndicos profissionais.
        </h1>
        <p class="landing-subtitle text-body-1 text-medium-emphasis mx-auto mb-8">
          Menos tempo procurando informação, menos retrabalho e mais controle sobre a rotina de cada condomínio que você administra.
        </p>
        <div class="d-flex flex-column flex-sm-row justify-center ga-3">
          <v-btn color="primary" size="large" to="/registrar" data-cy="btn-registrar">
            Criar conta grátis
          </v-btn>
          <v-btn variant="outlined" color="primary" size="large" to="/login">
            Já tenho conta
          </v-btn>
        </div>
      </div>
    </section>

    <!-- Benefícios -->
    <section class="landing-container py-12">
      <v-row justify="center">
        <v-col v-for="beneficio in beneficios" :key="beneficio.titulo" cols="12" sm="6" md="4">
          <v-card class="landing-card fill-height" variant="flat" border rounded="lg">
            <v-card-item>
              <div class="landing-icon mb-3">
                <v-icon :icon="beneficio.icone" size="28" color="primary" />
              </div>
              <h3 class="text-subtitle-1 font-weight-bold mb-1">{{ beneficio.titulo }}</h3>
              <p class="text-body-2 text-medium-emphasis">{{ beneficio.texto }}</p>
            </v-card-item>
          </v-card>
        </v-col>
      </v-row>
    </section>

    <!-- CTA final -->
    <section class="landing-cta">
      <div class="landing-container text-center py-12">
        <h2 class="text-h5 font-weight-bold mb-3">Comece a organizar seus condomínios hoje</h2>
        <p class="text-body-1 text-medium-emphasis mx-auto mb-6 landing-subtitle">
          Crie sua conta em poucos minutos e centralize toda a sua operação.
        </p>
        <v-btn color="primary" size="large" to="/registrar">Criar conta grátis</v-btn>
      </div>
    </section>

    <!-- Rodapé -->
    <footer class="landing-footer">
      <div class="landing-container text-center py-6">
        <span class="text-caption text-medium-emphasis">
          © 2026 euSíndico - organização para síndicos profissionais.
        </span>
      </div>
    </footer>
  </div>
</template>

<style scoped>
.landing {
  background-color: rgb(var(--v-theme-background));
}

.landing-container {
  width: 100%;
  max-width: 960px;
  margin-inline: auto;
  padding-inline: 20px;
}

.landing-header {
  background-color: rgb(var(--v-theme-surface));
  border-bottom: 1px solid rgba(var(--v-border-color), 0.08);
}

.landing-logo-mark {
  height: 20px;
  width: auto;
  display: block;
}

.landing-logo-hero {
  height: 96px;
  width: auto;
  display: block;
  margin-inline: auto;
}

.landing-hero {
  background-color: rgba(var(--v-theme-secondary), 0.1);
}

.landing-subtitle {
  max-width: 620px;
}

/* Círculo suave atrás do ícone de cada benefício — cor da marca, baixa opacidade. */
.landing-icon {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 52px;
  height: 52px;
  border-radius: 14px;
  background-color: rgba(var(--v-theme-secondary), 0.16);
}

.landing-card {
  transition: border-color 0.15s ease;
}

.landing-cta {
  background-color: rgb(var(--v-theme-surface));
  border-top: 1px solid rgba(var(--v-border-color), 0.08);
}

.landing-footer {
  background-color: rgb(var(--v-theme-background));
}
</style>
