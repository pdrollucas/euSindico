import { z } from 'zod'
import { ANO_MINIMO, periodoEhFuturo } from '@/utils/periodo'

// Espelha GerarRelatorioDto. Mês futuro depende do fuso da aplicação e o backend é a fonte da
// verdade; aqui evita só a ida e volta de um 400 previsível (o formulário já desabilita esses meses).
export const gerarRelatorioFormSchema = z
  .object({
    mes: z
      .number({ required_error: 'Selecione o mês', invalid_type_error: 'Selecione o mês' })
      .int()
      .min(1, 'Selecione o mês')
      .max(12, 'Selecione o mês'),
    ano: z
      .number({ required_error: 'Selecione o ano', invalid_type_error: 'Selecione o ano' })
      .int()
      .min(ANO_MINIMO, `O ano deve ser ${ANO_MINIMO} ou posterior`),
  })
  .refine(({ mes, ano }) => !periodoEhFuturo(mes, ano), {
    message: 'Não é possível gerar relatório de um mês futuro',
    path: ['mes'],
  })

// Espelha RelatorioDto. `geradoEm` chega como ISO do servidor (UTC, às vezes sem o "Z").
export const relatorioSchema = z.object({
  id: z.number(),
  mesReferencia: z.number(),
  anoReferencia: z.number(),
  nomeArquivo: z.string(),
  predioId: z.number(),
  predioNome: z.string(),
  geradoEm: z.string(),
})

// Espelha PagedResultDto<RelatorioDto> (RNF10).
export const relatoriosPaginadosSchema = z.object({
  items: z.array(relatorioSchema),
  page: z.number(),
  pageSize: z.number(),
  totalCount: z.number(),
  totalPages: z.number(),
})
