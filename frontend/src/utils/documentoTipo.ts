import type { TipoDocumento } from '@/types/documento.types'

// Textos e ícone de cada tela. Atas e normas compartilham a view e os componentes; só isto muda.
// Ambos os substantivos são femininos, então "enviada"/"removida" servem aos dois.
export interface ConfigTipoDocumento {
  titulo: string
  singular: string
  icone: string
  // Limite de quantidade do backend (RN40/RN41), mostrado no diálogo de envio.
  limite: string
  vazio: string
  vazioPodeEnviar: string
  enviada: string
  removida: string
}

export const TIPOS_DOCUMENTO: Record<TipoDocumento, ConfigTipoDocumento> = {
  atas: {
    titulo: 'Atas',
    singular: 'ata',
    icone: 'mdi-file-document-outline',
    limite: 'O limite é de 5 atas por mês, por prédio.',
    vazio: 'Este prédio ainda não tem atas.',
    vazioPodeEnviar: 'Este prédio ainda não tem atas. Envie a primeira!',
    enviada: 'Ata enviada',
    removida: 'Ata removida',
  },
  normas: {
    titulo: 'Normas',
    singular: 'norma',
    icone: 'mdi-file-document-multiple-outline',
    limite: 'O limite é de 10 normas por prédio.',
    vazio: 'Este prédio ainda não tem normas.',
    vazioPodeEnviar: 'Este prédio ainda não tem normas. Envie a primeira!',
    enviada: 'Norma enviada',
    removida: 'Norma removida',
  },
}
