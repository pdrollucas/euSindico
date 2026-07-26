import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import MockAdapter from 'axios-mock-adapter'
import api from './axios'
import { useAuthStore } from '@/stores/authStore'

// Testa o interceptor de verdade (não mocka o axios inteiro) — o MockAdapter substitui só a
// camada de transporte HTTP, então os interceptors registrados em axios.ts continuam rodando
// normalmente. Ver frontend/documentation/AUTHENTICATION.md, seção 5.
describe('plugins/axios', () => {
  let mock: MockAdapter

  beforeEach(() => {
    setActivePinia(createPinia())
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('anexa o Authorization header quando há accessToken', async () => {
    const authStore = useAuthStore()
    authStore.setAccessToken('token-abc')
    mock.onGet('/predios').reply(200, [])

    await api.get('/predios')

    expect(mock.history.get[0]?.headers?.Authorization).toBe('Bearer token-abc')
  })

  it('não anexa Authorization quando não há sessão', async () => {
    mock.onGet('/predios').reply(200, [])

    await api.get('/predios')

    expect(mock.history.get[0]?.headers?.Authorization).toBeUndefined()
  })

  it('em 401 numa rota protegida, renova o token e repete a requisição original', async () => {
    const authStore = useAuthStore()
    authStore.setAccessToken('token-expirado')

    mock
      .onGet('/predios')
      .replyOnce(401)
      .onGet('/predios')
      .replyOnce(200, [{ id: 1, nome: 'Edifício A' }])
    mock.onPost('/auth/refresh').replyOnce(200, { accessToken: 'token-novo' })

    const { data } = await api.get('/predios')

    expect(data).toEqual([{ id: 1, nome: 'Edifício A' }])
    expect(authStore.accessToken).toBe('token-novo')
    // A requisição repetida já deve carregar o novo token.
    expect(mock.history.get[1]?.headers?.Authorization).toBe('Bearer token-novo')
  })

  it('quando o refresh falha, propaga o erro original e limpa a sessão', async () => {
    const authStore = useAuthStore()
    authStore.setAccessToken('token-expirado')

    mock.onGet('/predios').reply(401)
    mock.onPost('/auth/refresh').reply(401)

    await expect(api.get('/predios')).rejects.toMatchObject({ response: { status: 401 } })
    expect(authStore.accessToken).toBeNull()
  })

  it('em 401 no /auth/login (credenciais inválidas), NÃO tenta renovar o token', async () => {
    mock.onPost('/auth/login').reply(401, { title: 'Credenciais inválidas', status: 401 })

    await expect(api.post('/auth/login', { email: 'a@b.com', senha: 'errada' })).rejects.toMatchObject(
      { response: { status: 401 } },
    )

    const chamadasRefresh = mock.history.post.filter((req) => req.url === '/auth/refresh')
    expect(chamadasRefresh).toHaveLength(0)
  })

  it('deduplica renovações concorrentes: duas requisições em 401 ao mesmo tempo geram só um refresh', async () => {
    const authStore = useAuthStore()
    authStore.setAccessToken('token-expirado')

    mock
      .onGet('/predios')
      .replyOnce(401)
      .onGet('/predios')
      .replyOnce(200, ['predio'])
    mock
      .onGet('/compromissos')
      .replyOnce(401)
      .onGet('/compromissos')
      .replyOnce(200, ['compromisso'])
    mock.onPost('/auth/refresh').replyOnce(() =>
      new Promise((resolve) => setTimeout(() => resolve([200, { accessToken: 'token-novo' }]), 10)),
    )

    const [predios, compromissos] = await Promise.all([
      api.get('/predios'),
      api.get('/compromissos'),
    ])

    expect(predios.data).toEqual(['predio'])
    expect(compromissos.data).toEqual(['compromisso'])
    const chamadasRefresh = mock.history.post.filter((req) => req.url === '/auth/refresh')
    expect(chamadasRefresh).toHaveLength(1)
  })
})
