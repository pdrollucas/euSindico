# Design — Frontend euSíndico

Este documento registra as **decisões visuais e de UX** e os **padrões reutilizáveis** do frontend — o *porquê* e o *como*, que não são deriváveis só do código. É o complemento visual da [ARCHITECTURE.md](ARCHITECTURE.md) (que descreve estrutura e camadas): quando os dois falam do mesmo assunto, a ARCHITECTURE manda na estrutura e este arquivo manda na aparência/experiência.

> **Fonte de verdade dos valores:** os valores concretos do tema (cores, etc.) vivem em [`src/plugins/vuetify.ts`](../src/plugins/vuetify.ts) — este documento aponta para lá e captura o *racional*, não os hex soltos, para não desatualizar. Se um valor aqui divergir do `vuetify.ts`, o `vuetify.ts` está certo.

Escrito após o redesign da landing e das telas de autenticação (cadastro, login e o fluxo de recuperação de senha). Deve crescer conforme as próximas telas (home, prédios, compromissos…) forem redesenhadas.

## Sumário

1. [Princípios](#1-princípios)
2. [Tema e paleta](#2-tema-e-paleta)
3. [Logos](#3-logos)
4. [Padrões de componente](#4-padrões-de-componente)
5. [Voz e conteúdo](#5-voz-e-conteúdo)

## 1. Princípios

Quatro princípios, em ordem de prioridade quando entrarem em conflito:

1. **Simples e consistente.** Toda tela nova deve parecer parte do mesmo produto que as anteriores. Prefira reaproveitar um padrão já existente (seção 4) a inventar um novo.
2. **Sem grandes efeitos visuais.** Nada de gradientes, animações chamativas, parallax ou sombras pesadas. O visual é limpo: superfícies planas, bordas finas, cor usada com intenção (ação = azul da marca), muito branco.
3. **Mobile-first** (RFC, RNF08). Desenhe primeiro para a coluna estreita; o desktop é a adaptação, não o contrário. Cada tela empilha em coluna única no celular. Cuidado com espaçamento em telas de ~360px (ver "Espaçamento" na seção 4).
4. **Consistência por fonte única.** O tema é centralizado em [`vuetify.ts`](../src/plugins/vuetify.ts) — cores e defaults saem de lá, não são reescritos tela a tela. É o que garante que "azul do botão" seja o mesmo azul em todo lugar.

## 2. Tema e paleta

Paleta derivada da **logo principal** (o azul institucional dela). Definida em [`vuetify.ts`](../src/plugins/vuetify.ts) como o tema `euSindico`. Hoje só há **tema claro** — não há dark mode.

| Token (Vuetify) | Papel | Racional |
|---|---|---|
| `primary` | Ações: botões, links de destaque, ícones ativos | Azul institucional da logo. É a única cor "forte" da interface — usada com parcimônia para marcar o que é clicável/importante. |
| `secondary` | Fundos suaves (hero, círculo atrás de ícones) em baixa opacidade | Azul claro da logo. Quase nunca usado cheio; entra como `rgba(..., 0.1–0.16)` para dar um tom de marca sem competir com o `primary`. |
| `background` | Fundo das páginas | Um branco levemente azulado, não `#FFFFFF` puro, para reduzir o contraste duro e dar sensação de calma. |
| `surface` | Cards, cabeçalhos, superfícies elevadas | Branco puro, para os cards "saltarem" suavemente do `background` levemente azulado. |

**Regra prática:** azul é sinal de ação. Se algo não é clicável nem é a marca, não deveria ser azul. Texto secundário usa `text-medium-emphasis`, não uma cor inventada.

## 3. Logos

Dois arquivos em [`src/assets/`](../src/assets/):

| Arquivo | Nome | Onde usar |
|---|---|---|
| `logo-eusindico.svg` | Principal (marca completa) | Momentos de destaque com espaço vertical: o hero da landing, telas de abertura. |
| `logo-eusindico-icon.svg` | Secundária (só o ícone) | Contextos compactos ao lado do wordmark "euSíndico": cabeçalho da landing, *auth chrome* (topo das telas de login/cadastro). |

Regra: onde a marca aparece pequena e ao lado do texto "euSíndico", use a **secundária**; onde ela é o elemento principal e tem espaço, use a **principal**.

## 4. Padrões de componente

Padrões já estabelecidos — reutilize antes de criar um novo. Referências de implementação entre parênteses.

- **Card de conteúdo** (benefícios da [`LandingView`](../src/views/landing/LandingView.vue)): `variant="flat"` + `border` + `rounded="lg"`. Sem sombra pesada. Ícone dentro de um círculo suave (`secondary` em ~0.16 de opacidade), título `subtitle-1` em negrito, texto `body-2` com `text-medium-emphasis`.
- **Card de formulário** (telas de auth — [`RegistrarView`](../src/views/auth/RegistrarView.vue), [`LoginView`](../src/views/auth/LoginView.vue)): `rounded="lg"` + `elevation="2"`. Título `text-h5` em negrito + subtítulo curto explicando o valor da ação. Erros via `v-alert` `variant="tonal"` `type="error"`. Links de rodapé em azul da marca (`text-primary`): as telas se cruzam ("Não tem uma conta? Criar conta" ↔ "Já tem uma conta? Entrar"); quando há mais de uma ação (ex: login), a secundária ("Esqueci minha senha") fica acima e o cruzamento de conta abaixo.
- **Auth chrome** ([`AuthLayout.vue`](../src/layouts/AuthLayout.vue)): a identidade da marca (logo secundária + wordmark, clicável de volta para a landing `/`) fica no **layout compartilhado**, não em cada view — assim login e cadastro ficam idênticos no topo. Toda tela de autenticação herda isso de graça.
- **Botões**: ação primária sempre `color="primary"`; em formulário, `size="large"` + `block`. Ação secundária ao lado, `variant="outlined"`. Nunca dois botões preenchidos concorrendo — só um "peso" primário por bloco.
- **Campos de formulário** ([`RegistrarForm.vue`](../src/components/auth/RegistrarForm.vue), [`LoginForm.vue`](../src/components/auth/LoginForm.vue)): `prepend-inner-icon` para dar contexto ao campo; senha com toggle de mostrar/ocultar (`append-inner-icon` olho). A dica das regras de senha (`hint` `persistent-hint`, RNF04) aparece **só quando o usuário cria/define uma senha** (cadastro, redefinição), nunca no login. O `autocomplete` depende do contexto: `new-password` ao criar/redefinir, `current-password` no login, `one-time-code` no campo de código, `name`/`email` nos demais — para os gerenciadores distinguirem criar de entrar.
- **Botão com cooldown / contagem regressiva** (fluxo de recuperação de senha — [`EsqueciSenhaForm.vue`](../src/components/auth/EsqueciSenhaForm.vue), [`VerificarCodigoView.vue`](../src/views/auth/VerificarCodigoView.vue)): ações sujeitas a *rate limit* (enviar/reenviar código) ficam **desabilitadas com a contagem no próprio rótulo** ("Aguarde 4:59" / "Reenviar e-mail em 4:59") até zerar. A contagem vem do composable [`useContagemRegressiva`](../src/composables/useContagemRegressiva.ts) sobre um timestamp guardado na store; o formato `m:ss` vem de [`utils/tempo.ts`](../src/utils/tempo.ts). É UX que espelha o cooldown real do backend (RN15) — ver [SECURITY.md](SECURITY.md), seção 4.
- **Campo de código** ([`VerificarCodigoForm.vue`](../src/components/auth/VerificarCodigoForm.vue)): entrada curta de 6 caracteres em caixa alta, centralizada e com `letter-spacing` — puramente apresentação (`text-transform`/CSS); o backend normaliza caixa e espaços, então o valor real não depende disso. `maxlength="6"` + `autocomplete="one-time-code"`.
- **Espaçamento (lições do mobile)**:
  - Não empilhe padding: `v-card-text`/`v-card-item` já trazem o deles — evite somar um `pa-*` no `v-card` por cima (rouba largura em telas estreitas).
  - Não zere o respiro do cabeçalho: `pb-0` no `v-card-item` cola o subtítulo no primeiro campo. Deixe o padding padrão separar header e formulário.
  - `persistent-hint` já reserva uma linha — considere isso antes de somar `mt-*` grande no botão seguinte.
- **`data-cy`**: todo elemento interativo relevante (inputs, botões, links, alertas) leva um `data-cy` estável, usado pelos testes Cypress. Ao redesenhar, **preserve os `data-cy` existentes** — mudar markup não deve quebrar teste. Ver [TEST.md](TEST.md).

## 5. Voz e conteúdo

- **Evite "operacional".** Para um síndico, "operacional" pode remeter a portaria, câmeras, controle de acesso — que estão *fora do escopo* (RFC 2.6). Fale das tarefas concretas: "compromissos, visitas, documentos, relatórios", "o dia a dia do síndico".
- **Posicionamento:** o euSíndico organiza o trabalho **do síndico**, e **não** faz gestão financeira do condomínio (é o contraste central com os concorrentes — RFC 1.3). Reforce isso, mas **em um único lugar por tela** para não repetir.
- **Hero mostra resultado; cards provam.** O título/subtítulo do hero fala do *resultado* para o usuário (menos retrabalho, mais controle) e evita listar features; a enumeração concreta (compromissos, prédios, documentos, relatórios) vive nos cards. Isso evita repetir a mesma lista duas vezes na mesma página.
- **Copy enxuta.** Textos curtos, uma ideia por bloco. Frase longa aumenta a chance de repetir o que outra seção já disse.
- **Recuperação de senha — copy condicional.** As telas de recuperação nunca afirmam que o e-mail existe: use "se houver uma conta com esse e-mail, enviaremos/enviamos um código", não "enviamos um código para X". Confirmar o envio revelaria quais e-mails estão cadastrados — é uma exigência de segurança, não só de estilo (anti-enumeração, ver [SECURITY.md](SECURITY.md), seção 4).

## Referências

- [ARCHITECTURE.md](ARCHITECTURE.md) — estrutura, camadas, layouts, roteamento (landing vs. área logada).
- [`src/plugins/vuetify.ts`](../src/plugins/vuetify.ts) — fonte de verdade do tema (cores).
- [TEST.md](TEST.md) — convenção de `data-cy` e o que é testado em cada nível.
- [RFC.md](../../documentation/RFC/RFC.md) — problema, posicionamento, RNF04 (senha), RNF08 (mobile-first), escopo (2.6).
