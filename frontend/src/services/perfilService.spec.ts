import { describe, it, expect, beforeEach, afterEach } from 'vitest'
import { setActivePinia, createPinia } from 'pinia'
import MockAdapter from 'axios-mock-adapter'
import { ZodError } from 'zod'
import api from '@/plugins/axios'
import { perfilService } from './perfilService'

const usuario = {
  id: 1,
  nome: 'João da Silva',
  email: 'joao@exemplo.com',
  criadoEm: '2026-01-01T12:00:00Z',
}

describe('perfilService', () => {
  let mock: MockAdapter

  beforeEach(() => {
    // O interceptor de request do Axios lê o authStore.
    setActivePinia(createPinia())
    mock = new MockAdapter(api)
  })

  afterEach(() => {
    mock.restore()
  })

  it('obterPerfil usa GET /perfil e valida a resposta', async () => {
    mock.onGet('/perfil').reply(200, usuario)

    await expect(perfilService.obterPerfil()).resolves.toEqual(usuario)
  })

  it('obterPerfil rejeita uma resposta fora do contrato', async () => {
    mock.onGet('/perfil').reply(200, { id: 1 })

    await expect(perfilService.obterPerfil()).rejects.toBeInstanceOf(ZodError)
  })

  it('atualizarPerfil usa PUT /perfil mandando só o nome', async () => {
    mock.onPut('/perfil').reply(200, { ...usuario, nome: 'João Souza' })

    const resultado = await perfilService.atualizarPerfil({ nome: 'João Souza' })

    expect(JSON.parse(mock.history.put[0]?.data as string)).toEqual({ nome: 'João Souza' })
    expect(resultado.nome).toBe('João Souza')
  })

  it('atualizarPerfil não chama a API com nome inválido', async () => {
    await expect(perfilService.atualizarPerfil({ nome: '' })).rejects.toBeInstanceOf(ZodError)

    expect(mock.history.put).toHaveLength(0)
  })

  it('solicitarTrocaEmail usa POST /perfil/email/solicitar com novo e-mail e senha', async () => {
    mock.onPost('/perfil/email/solicitar').reply(204)

    await perfilService.solicitarTrocaEmail({ novoEmail: 'novo@exemplo.com', senhaAtual: 'Senha@123' })

    expect(JSON.parse(mock.history.post[0]?.data as string)).toEqual({
      novoEmail: 'novo@exemplo.com',
      senhaAtual: 'Senha@123',
    })
  })

  it('solicitarTrocaEmail não chama a API com e-mail inválido', async () => {
    await expect(
      perfilService.solicitarTrocaEmail({ novoEmail: 'invalido', senhaAtual: 'Senha@123' }),
    ).rejects.toBeInstanceOf(ZodError)

    expect(mock.history.post).toHaveLength(0)
  })

  it('confirmarTrocaEmail usa POST /perfil/email/confirmar e devolve o perfil atualizado', async () => {
    mock.onPost('/perfil/email/confirmar').reply(200, { ...usuario, email: 'novo@exemplo.com' })

    const resultado = await perfilService.confirmarTrocaEmail({ codigo: 'ABC234' })

    expect(JSON.parse(mock.history.post[0]?.data as string)).toEqual({ codigo: 'ABC234' })
    expect(resultado.email).toBe('novo@exemplo.com')
  })

  it('confirmarTrocaEmail não chama a API com código de tamanho errado', async () => {
    await expect(perfilService.confirmarTrocaEmail({ codigo: 'AB' })).rejects.toBeInstanceOf(ZodError)

    expect(mock.history.post).toHaveLength(0)
  })

  it('alterarSenha usa PUT /perfil/senha mandando só senha atual e nova senha', async () => {
    mock.onPut('/perfil/senha').reply(204)

    await perfilService.alterarSenha({ senhaAtual: 'Antiga@123', novaSenha: 'Nova@12345' })

    expect(JSON.parse(mock.history.put[0]?.data as string)).toEqual({
      senhaAtual: 'Antiga@123',
      novaSenha: 'Nova@12345',
    })
  })

  it('alterarSenha não chama a API com nova senha fraca', async () => {
    await expect(
      perfilService.alterarSenha({ senhaAtual: 'Antiga@123', novaSenha: 'fraca' }),
    ).rejects.toBeInstanceOf(ZodError)

    expect(mock.history.put).toHaveLength(0)
  })

  it('excluirConta usa DELETE /perfil com a senha no corpo', async () => {
    mock.onDelete('/perfil').reply(204)

    await perfilService.excluirConta({ senha: 'Senha@123' })

    expect(JSON.parse(mock.history.delete[0]?.data as string)).toEqual({ senha: 'Senha@123' })
  })

  it('excluirConta não chama a API sem senha', async () => {
    await expect(perfilService.excluirConta({ senha: '' })).rejects.toBeInstanceOf(ZodError)

    expect(mock.history.delete).toHaveLength(0)
  })
})
