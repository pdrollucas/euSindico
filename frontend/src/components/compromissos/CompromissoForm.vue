<script setup lang="ts">
import { computed, nextTick, ref, watch } from 'vue'
import { useForm } from 'vee-validate'
import { toTypedSchema } from '@vee-validate/zod'
import { compromissoFormSchema } from '@/schemas/compromisso.schema'
import { mascararHorario } from '@/utils/dataHora'
import { mascararMoedaDigitada } from '@/utils/moeda'
import { PAPEL_PREDIO } from '@/types/predio.types'
import type { Predio } from '@/types/predio.types'
import type { EquipeMembro } from '@/types/equipe.types'
import type { CompromissoFormValues } from '@/types/compromisso.types'

// Reaproveitado na criação e na edição — mesmo formato (CompromissoFormDto no backend). Componente
// "burro": quem busca prédios/equipe é a view; aqui só as regras de tela por papel (RN20, RN24).
const props = defineProps<{
  valoresIniciais?: Partial<CompromissoFormValues>
  predios: Predio[]
  membros: EquipeMembro[]
  perfilId: number | null
  // Edição: o responsável atual pode já ter saído da equipe (RN22) — continua selecionável para
  // não ser trocado em silêncio.
  responsavelInicial?: { usuarioId: number; nome: string }
  salvando?: boolean
  modoEdicao?: boolean
}>()
const emit = defineEmits<{
  submit: [payload: CompromissoFormValues]
  'predio-alterado': [predioId: number]
}>()

const { defineField, handleSubmit, errors, setValues } = useForm({
  validationSchema: toTypedSchema(compromissoFormSchema),
  initialValues: props.valoresIniciais,
})

const [predioId, predioIdAttrs] = defineField('predioId')
const [titulo, tituloAttrs] = defineField('titulo')
const [dataCompromisso, dataAttrs] = defineField('dataCompromisso')
const [horarioCompromisso, horarioAttrs] = defineField('horarioCompromisso')
const [responsavelUsuarioId, responsavelAttrs] = defineField('responsavelUsuarioId')
const [nomePrestadorServico, prestadorAttrs] = defineField('nomePrestadorServico')
const [orcamento, orcamentoAttrs] = defineField('orcamento')
const [detalhes, detalhesAttrs] = defineField('detalhes')

// Os valores iniciais só chegam depois de um GET assíncrono (modo edição) — o form já existe
// antes disso, então precisa reagir quando eles carregarem.
const predioOriginal = ref<number | null>(props.valoresIniciais?.predioId ?? null)
watch(
  () => props.valoresIniciais,
  (valores) => {
    if (valores) {
      predioOriginal.value = valores.predioId ?? null
      setValues(valores)
    }
  },
)

const predioSelecionado = computed(() => props.predios.find((p) => p.id === predioId.value))

// RN20: Colaborador não escolhe responsável (é sempre ele); Síndico/Gestor escolhem.
const podeEscolherResponsavel = computed(
  () =>
    predioSelecionado.value !== undefined &&
    predioSelecionado.value.papel !== PAPEL_PREDIO.COLABORADOR,
)

// RN24: só Síndico/Gestor movem de prédio, e o destino também precisa ser Síndico/Gestor. Na edição
// de um compromisso de prédio onde é Colaborador, o campo trava; no destino, só aparecem prédios
// onde o papel permite.
const predioOriginalEhColaborador = computed(
  () =>
    props.modoEdicao &&
    props.predios.find((p) => p.id === predioOriginal.value)?.papel === PAPEL_PREDIO.COLABORADOR,
)

const itensPredio = computed(() =>
  props.predios
    .filter(
      (p) =>
        !props.modoEdicao || p.papel !== PAPEL_PREDIO.COLABORADOR || p.id === predioOriginal.value,
    )
    .map((p) => ({ title: p.nome, value: p.id })),
)

const itensResponsavel = computed(() => {
  const itens = props.membros.map((m) => ({
    title: m.usuarioId === props.perfilId ? `${m.nome} (você)` : m.nome,
    value: m.usuarioId,
  }))
  const inicial = props.responsavelInicial
  if (inicial && !itens.some((i) => i.value === inicial.usuarioId)) {
    itens.push({ title: `${inicial.nome} (fora da equipe)`, value: inicial.usuarioId })
  }
  return itens
})

const predioMudouPeloUsuario = ref(false)
watch(
  predioId,
  (atual, anterior) => {
    if (typeof atual === 'number') {
      emit('predio-alterado', atual)
    }
    if (anterior != null && atual != null && atual !== anterior) {
      predioMudouPeloUsuario.value = true
    }
  },
  { immediate: true },
)

// Padrão do responsável é o próprio autor. Ao trocar de prédio, volta para o autor se o
// responsável escolhido não pertence à equipe do destino (o backend devolveria 400, RN24).
watch(
  () => [props.membros, podeEscolherResponsavel.value, props.perfilId] as const,
  () => {
    if (!podeEscolherResponsavel.value || props.perfilId == null) {
      return
    }
    const ids = props.membros.map((m) => m.usuarioId)
    const semResponsavel = responsavelUsuarioId.value == null
    const foraDaEquipeDestino =
      predioMudouPeloUsuario.value &&
      responsavelUsuarioId.value != null &&
      !ids.includes(responsavelUsuarioId.value)
    if (semResponsavel || foraDaEquipeDestino) {
      responsavelUsuarioId.value = props.perfilId
    }
  },
  { immediate: true },
)

// Aplica a máscara de horário e devolve o valor mascarado ao próprio <input>: quando o texto
// mascarado é igual ao anterior (ex: uma letra digitada), o modelo não muda, o Vue não re-renderiza
// e a letra ficaria visível no campo.
const horarioCampo = ref<{ $el: HTMLElement } | null>(null)
function onHorarioDigitado(valor: string | null) {
  const mascarado = mascararHorario(valor)
  horarioCompromisso.value = mascarado
  nextTick(() => {
    const input = horarioCampo.value?.$el.querySelector('input')
    if (input && input.value !== mascarado) {
      input.value = mascarado
    }
  })
}

// Mesma máscara de moeda do orçamento previsto do planejamento (PlanejamentoForm), com o mesmo
// cuidado de devolver o valor mascarado ao <input>.
const orcamentoCampo = ref<{ $el: HTMLElement } | null>(null)
function onOrcamentoDigitado(valor: string | null) {
  const mascarado = mascararMoedaDigitada(valor, orcamento.value ?? '')
  orcamento.value = mascarado
  nextTick(() => {
    const input = orcamentoCampo.value?.$el.querySelector('input')
    if (input && input.value !== mascarado) {
      input.value = mascarado
    }
  })
}

// O ícone nativo do <input type="date"> fica escondido (já existe o `prepend-inner-icon`, e os dois
// apareciam juntos). Sem ele o único ponto clicável seria o texto do campo, então o clique em
// qualquer parte do campo abre o seletor. `showPicker` pode lançar (sem gesto do usuário, navegador
// sem suporte) — o foco no input continua funcionando como antes.
function abrirSeletor(evento: Event) {
  const input = (evento.currentTarget as HTMLElement).querySelector('input')
  try {
    input?.showPicker?.()
  } catch {
    // Sem seletor nativo: o usuário digita.
  }
}

const onSubmit = handleSubmit((values) => {
  emit('submit', {
    ...values,
    // Colaborador nunca envia responsável (indicar outro seria 403, RN20).
    responsavelUsuarioId: podeEscolherResponsavel.value ? values.responsavelUsuarioId : undefined,
  })
})
</script>

<template>
  <v-form class="compromisso-form" @submit.prevent="onSubmit">
    <v-text-field
      v-model="titulo"
      v-bind="tituloAttrs"
      data-cy="titulo"
      label="Título"
      placeholder="Ex: Reunião de condomínio"
      autocomplete="off"
      prepend-inner-icon="mdi-text-short"
      :error-messages="errors.titulo"
    />
    <v-select
      v-model="predioId"
      v-bind="predioIdAttrs"
      data-cy="predio"
      label="Condomínio"
      placeholder="Selecione o condomínio"
      :items="itensPredio"
      :disabled="predioOriginalEhColaborador"
      prepend-inner-icon="mdi-office-building-outline"
      :error-messages="errors.predioId"
    />
    <v-text-field
      v-model="dataCompromisso"
      v-bind="dataAttrs"
      data-cy="data"
      @click="abrirSeletor"
      type="date"
      label="Data"
      persistent-placeholder
      prepend-inner-icon="mdi-calendar-outline"
      :error-messages="errors.dataCompromisso"
    />
    <!-- Digitado, não escolhido num relógio: `inputmode="numeric"` abre o teclado numérico no
         celular e a máscara põe o ":" sozinha ("1830" -> "18:30"). -->
    <v-text-field
      ref="horarioCampo"
      v-bind="horarioAttrs"
      :model-value="horarioCompromisso"
      data-cy="horario"
      inputmode="numeric"
      maxlength="5"
      label="Horário"
      placeholder="HH:mm"
      persistent-placeholder
      autocomplete="off"
      prepend-inner-icon="mdi-clock-outline"
      :error-messages="errors.horarioCompromisso"
      @update:model-value="onHorarioDigitado"
    />
    <v-select
      v-if="podeEscolherResponsavel"
      v-model="responsavelUsuarioId"
      v-bind="responsavelAttrs"
      data-cy="responsavel"
      label="Responsável"
      placeholder="Selecione o responsável"
      :items="itensResponsavel"
      prepend-inner-icon="mdi-account-outline"
      :error-messages="errors.responsavelUsuarioId"
    />
    <v-text-field
      v-model="nomePrestadorServico"
      v-bind="prestadorAttrs"
      data-cy="prestador"
      label="Prestador de serviços"
      placeholder="Digite o nome (opcional)"
      autocomplete="off"
      prepend-inner-icon="mdi-account-hard-hat-outline"
      :error-messages="errors.nomePrestadorServico"
    />
    <v-text-field
      ref="orcamentoCampo"
      v-bind="orcamentoAttrs"
      :model-value="orcamento"
      data-cy="orcamento"
      inputmode="numeric"
      maxlength="14"
      label="Orçamento"
      placeholder="0,00 (opcional)"
      persistent-placeholder
      autocomplete="off"
      prefix="R$"
      prepend-inner-icon="mdi-cash"
      :error-messages="errors.orcamento"
      @update:model-value="onOrcamentoDigitado"
    />
    <v-textarea
      v-model="detalhes"
      v-bind="detalhesAttrs"
      data-cy="detalhes"
      label="Detalhes"
      placeholder="Digite os detalhes (opcional)"
      autocomplete="off"
      rows="2"
      auto-grow
      prepend-inner-icon="mdi-text-box-outline"
      :error-messages="errors.detalhes"
    />

    <div class="compromisso-form__acao">
      <v-btn
        type="submit"
        color="primary"
        size="large"
        block
        :loading="salvando"
        data-cy="btn-salvar-compromisso"
      >
        {{ modoEdicao ? 'Salvar alterações' : 'Criar compromisso' }}
      </v-btn>
    </div>
  </v-form>
</template>

<style scoped>
/* Esconde o indicador nativo de calendário (duplicava o ícone à esquerda). */
.compromisso-form :deep(input[type='date']::-webkit-calendar-picker-indicator) {
  display: none;
}

/* Botão fixo no rodapé enquanto o formulário rola (wireframe "Criação de compromisso") — fundo
   sólido para não deixar os campos aparecerem por baixo. */
.compromisso-form__acao {
  position: sticky;
  bottom: 0;
  z-index: 1;
  margin-inline: -16px;
  padding: 12px 16px;
  background: rgb(var(--v-theme-background));
}
</style>
