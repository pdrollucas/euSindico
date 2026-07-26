import api from '@/plugins/axios'
import {
  loginRequestSchema,
  accessTokenResponseSchema,
  registrarRequestSchema,
  esqueciSenhaRequestSchema,
  verificarCodigoRequestSchema,
  redefinirSenhaRequestSchema,
} from '@/schemas/auth.schema'
import { usuarioSchema } from '@/schemas/usuario.schema'
import type {
  LoginRequest,
  AccessTokenResponse,
  RegistrarRequest,
  EsqueciSenhaRequest,
  VerificarCodigoRequest,
  RedefinirSenhaRequest,
} from '@/types/auth.types'
import type { Usuario } from '@/types/usuario.types'

// Uma função por endpoint de /auth/* (AuthController do backend) — ver
// frontend/documentation/ARCHITECTURE.md, seção "services/". Cada função valida a resposta
// com o schema Zod correspondente antes de devolvê-la tipada; não sabe onde o token é guardado.

async function login(payload: LoginRequest): Promise<AccessTokenResponse> {
  const body = loginRequestSchema.parse(payload)
  const { data } = await api.post('/auth/login', body)
  return accessTokenResponseSchema.parse(data)
}

async function refresh(): Promise<AccessTokenResponse> {
  const { data } = await api.post('/auth/refresh')
  return accessTokenResponseSchema.parse(data)
}

async function logout(): Promise<void> {
  await api.post('/auth/logout')
}

async function registrar(payload: RegistrarRequest): Promise<Usuario> {
  const body = registrarRequestSchema.parse(payload)
  const { data } = await api.post('/auth/registrar', body)
  return usuarioSchema.parse(data)
}

async function esqueciSenha(payload: EsqueciSenhaRequest): Promise<void> {
  const body = esqueciSenhaRequestSchema.parse(payload)
  await api.post('/auth/esqueci-senha', body)
}

async function verificarCodigo(payload: VerificarCodigoRequest): Promise<void> {
  const body = verificarCodigoRequestSchema.parse(payload)
  await api.post('/auth/verificar-codigo', body)
}

async function redefinirSenha(payload: RedefinirSenhaRequest): Promise<void> {
  const body = redefinirSenhaRequestSchema.parse(payload)
  await api.post('/auth/redefinir-senha', body)
}

export const authService = {
  login,
  refresh,
  logout,
  registrar,
  esqueciSenha,
  verificarCodigo,
  redefinirSenha,
}
