<script setup lang="ts">
import { onMounted, ref } from 'vue'
import QRCode from 'qrcode'

// QR code "continue no celular" sob os formulários de login e cadastro, só no desktop (ver
// DESIGN.md, "Layout responsivo"). Aponta para a mesma tela em que está; a origem vem do
// navegador, então nenhum domínio fica fixo no código.
const props = defineProps<{ caminho: '/login' | '/registrar' }>()

const imagem = ref<string | null>(null)

onMounted(async () => {
  try {
    imagem.value = await QRCode.toDataURL(`${window.location.origin}${props.caminho}`, {
      margin: 1,
      width: 112,
      color: { dark: '#1B2536', light: '#FFFFFF' },
    })
  } catch {
    // Silencioso de propósito: sem o QR code a tela continua completa.
  }
})
</script>

<template>
  <div
    v-if="imagem"
    class="qrcode-celular d-none d-md-flex align-center ga-4 mt-6 pa-4 rounded-lg"
    data-cy="qrcode-celular"
  >
    <img
      :src="imagem"
      alt="QR code para abrir esta tela no celular"
      class="qrcode-celular-imagem"
    />
    <div class="text-body-2">
      <p class="font-weight-medium mb-1">Prefere usar no celular?</p>
      <p class="text-medium-emphasis">
        Aponte a câmera do celular para o código e continue por lá.
      </p>
    </div>
  </div>
</template>

<style scoped>
.qrcode-celular {
  border: 1px solid rgba(27, 37, 54, 0.08);
  background: rgb(var(--v-theme-surface));
}

.qrcode-celular-imagem {
  width: 112px;
  height: 112px;
  flex: none;
  display: block;
}
</style>
