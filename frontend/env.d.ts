/// <reference types="vite/client" />

interface ImportMetaEnv {
  // URL base da API (nunca hardcoded — ver frontend/documentation/SECURITY.md, seção 8)
  readonly VITE_API_BASE_URL: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}
