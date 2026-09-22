// Formato mínimo de erro do backend — só `title`/`status` (ver ARCHITECTURE.md, seção 6).
export interface ErroApiDto {
  title?: string
  status?: number
}
