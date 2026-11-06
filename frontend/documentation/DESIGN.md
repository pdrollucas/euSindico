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
- **FAB (ação flutuante)** — padrão da ação principal de uma tela da área logada: botão circular fixo no canto inferior direito **da coluna de conteúdo** — no celular; no desktop a ação vai para o cabeçalho (ver "Cabeçalho de página" em "Layout responsivo") (`right: var(--app-fab-right)`, nunca `right: 24px` — ver "Layout responsivo"). Na [`HomeView`](../src/views/home/HomeView.vue) (só existe no celular) é o **Logout** (`color="error"`, `mdi-logout`) — no desktop o "Sair" fica no rail; nas telas filhas é a ação de "adicionar/criar/gerar" do módulo. No máximo um FAB por tela (ver [ARCHITECTURE.md](ARCHITECTURE.md), seção 5).
- **Layout responsivo (tablet e desktop)** — o celular é a referência; a partir do breakpoint `md` do Vuetify (840px) a área logada se adapta, sem mudar o fluxo mobile:
  - **Largura da tela:** toda view logada usa `max-width: var(--app-largura-conteudo)` + `margin-inline: auto` — 480px abaixo de 840px; a partir dele, a largura disponível até 1200px (só monitores grandes veem margem). As variáveis vivem em [`src/assets/main.css`](../src/assets/main.css); tela nova usa a variável, não um número.
  - **Cabeçalho de borda a borda:** no desktop o fundo branco e a linha inferior do `AppHeader` vão até as bordas da área ao lado do rail (sombras com spread grande + `clip-path`, sem `width: 100dvw`, que somaria o rail e a barra de rolagem e criaria rolagem horizontal). Os botões e o título continuam alinhados com o conteúdo.
  - **Listas em grade** (`.app-grade`, em `main.css`): no desktop o contêiner dos cards vira uma grade de 2–3 colunas (`minmax(320px, 1fr)`); no celular não muda nada. O espaço vertical continua vindo do `mb-3` dos cards, e o que deve ocupar a linha inteira (ex: "Carregar mais") leva `.app-grade-linha`. Usado nas listas (prédios, compromissos, planejamentos, relatórios, atas/normas, equipe) e nos hubs de cards (detalhe do prédio, Configurações).
  - **Bloco de formulário** (`.app-bloco-form`): nas telas de formulário/detalhe (detalhe de compromisso e planejamento, telas de configuração) o conteúdo abaixo do cabeçalho fica num bloco `d-flex flex-column` com no máximo 720px no desktop, centralizado — a tela ocupa a largura toda, mas campo largo demais piora a leitura.
  - **Navigation rail** ([`AppNavRail.vue`](../src/components/common/AppNavRail.vue)): barra fixa à esquerda (88px), só a partir de 840px. No topo, a marca (logo secundária sobre o texto "euSíndico", como no cabeçalho da Home); no meio exato da altura, Compromissos, Prédios e Configurações (ícone sobre rótulo curto, 28px entre eles); no rodapé, **Sair**, em `error`. O item ativo ganha uma pílula em `secondary` a ~0.16 e é marcado pelo prefixo do caminho (`/predios/5/atas` marca Prédios). Os módulos de um prédio não entram no rail.
  - **Sem Home no desktop:** a tela inicial é Compromissos (ver [ARCHITECTURE.md](ARCHITECTURE.md), seção 5). Somem também o botão de Home do `AppHeader` e, nas telas de topo (cujo "voltar" iria para a Home), o próprio "voltar" — espaçadores mantêm o título centralizado. Nas subtelas o "voltar" continua.
  - **Cabeçalho de página:** no desktop o `AppHeader` deixa de ser o do celular (título centralizado + atalho da Home) e vira cabeçalho de página: voltar (quando há para onde), título à esquerda em `text-title-large` negrito e, à direita, o slot `#acoes` com a ação principal da tela (botão `color="primary"` com ícone e texto, ex: "+ Novo compromisso"). No desktop essa ação **substitui o FAB**; a view continua passando o slot sempre, e o celular simplesmente não o renderiza.
  - **Tabela no lugar dos cards** ([`CompromissosTabela.vue`](../src/components/compromissos/CompromissosTabela.vue)): no desktop a lista de compromissos é um `v-table` (`hover`, `density="comfortable"`) dentro de um card `flat` + `border` + `rounded="lg"`, com colunas fixas e a de responsável só no escopo "todos". A linha inteira abre o detalhe; o título é o link real (teclado); checkbox e lixeira param o clique. Mesmos `data-cy` do card do celular. Sem ordenação pelo cabeçalho: a lista é paginada no backend, e ordenar só o que já veio enganaria.
  - **Filtros na barra** ([`CompromissoFiltrosBarra.vue`](../src/components/compromissos/CompromissoFiltrosBarra.vue)): no desktop a busca e os filtros ficam à vista, no lugar do ícone que abre a folha, numa grade com lugar fixo para cada peça: até 1279px, duas linhas em 4 colunas iguais (busca + escopo/"Limpar filtros" em cima, os 4 seletores embaixo); a partir de 1280px, uma linha só. Nada aparece ou some conforme o filtro, para a barra não "pular": o "Limpar filtros" fica desabilitado sem filtro ativo e mês/ano ficam desabilitados em "Hoje". Sem chips: o próprio filtro já mostra o valor. As regras (ano/mês andam juntos etc.) são as mesmas da folha, em [`utils/filtrosCompromisso.ts`](../src/utils/filtrosCompromisso.ts).
  - **Até agora só Compromissos** usa tabela, filtros na barra e ação no cabeçalho; as demais listas seguem com cards em grade e FAB no desktop até serem migradas.
  - **QR code "continue no celular"** ([`QrCodeCelular.vue`](../src/components/auth/QrCodeCelular.vue)): sob os formulários de login e cadastro, só no desktop, apontando para a mesma tela (`/login` ou `/registrar`) a partir da origem do navegador. Não vai para a landing (página de apresentação) nem para a área logada.
  - **O que não muda:** diálogos (`max-width="400"`), bottom sheets e telas de auth/landing — já se comportam bem em tela larga.
- **Campos de formulário** ([`RegistrarForm.vue`](../src/components/auth/RegistrarForm.vue), [`LoginForm.vue`](../src/components/auth/LoginForm.vue)): `prepend-inner-icon` para dar contexto ao campo; senha com toggle de mostrar/ocultar (`append-inner-icon` olho). A dica das regras de senha (`hint` `persistent-hint`, RNF04) aparece **só quando o usuário cria/define uma senha** (cadastro, redefinição), nunca no login. O `autocomplete` depende do contexto: `new-password` ao criar/redefinir, `current-password` no login, `one-time-code` no campo de código, `name`/`email` nos demais — para os gerenciadores distinguirem criar de entrar.
- **Botão com cooldown / contagem regressiva** (fluxo de recuperação de senha — [`EsqueciSenhaForm.vue`](../src/components/auth/EsqueciSenhaForm.vue), [`VerificarCodigoView.vue`](../src/views/auth/VerificarCodigoView.vue)): ações sujeitas a *rate limit* (enviar/reenviar código) ficam **desabilitadas com a contagem no próprio rótulo** ("Aguarde 4:59" / "Reenviar e-mail em 4:59") até zerar. A contagem vem do composable [`useContagemRegressiva`](../src/composables/useContagemRegressiva.ts) sobre um timestamp guardado na store; o formato `m:ss` vem de [`utils/tempo.ts`](../src/utils/tempo.ts). É UX que espelha o cooldown real do backend (RN15) — ver [SECURITY.md](SECURITY.md), seção 4.
- **Campo de código** ([`VerificarCodigoForm.vue`](../src/components/auth/VerificarCodigoForm.vue)): entrada curta de 6 caracteres em caixa alta, centralizada e com `letter-spacing` — puramente apresentação (`text-transform`/CSS); o backend normaliza caixa e espaços, então o valor real não depende disso. `maxlength="6"` + `autocomplete="one-time-code"`.
- **Espaçamento (lições do mobile)**:
  - Não empilhe padding: `v-card-text`/`v-card-item` já trazem o deles — evite somar um `pa-*` no `v-card` por cima (rouba largura em telas estreitas).
  - Não zere o respiro do cabeçalho: `pb-0` no `v-card-item` cola o subtítulo no primeiro campo. Deixe o padding padrão separar header e formulário.
  - `persistent-hint` já reserva uma linha — considere isso antes de somar `mt-*` grande no botão seguinte.
- **Item de lista com ações** ([`CompromissoListItem.vue`](../src/components/compromissos/CompromissoListItem.vue)): card `variant="flat"` + `border` + `rounded="lg"`; o título/subtítulo é o link para o detalhe, checkbox e lixeira (`color="error"`) ficam ao lado, fora do link (clicar no checkbox não navega). Ações destrutivas sempre passam por `ConfirmDialog`.
- **Saída animada de item (concluir)**: ao marcar o checkbox, o título é riscado por uma linha que cresce da esquerda (350 ms) e, após uma pausa curta, o card some com fade (300 ms); os de baixo sobem deslizando (`TransitionGroup`). É feedback funcional, não decoração — por isso é curto e sem sombras/escala. Respeita `prefers-reduced-motion` (sem animação, o item sai direto). Os tempos vivem em [`utils/animacao.ts`](../src/utils/animacao.ts) porque o CSS e o `setTimeout` da view precisam concordar. Depois da saída, um **snackbar com "Desfazer"** reverte a ação (o mesmo vale para reabrir).
- **Card que abre arquivo + ações** ([`RelatorioListItem.vue`](../src/components/relatorios/RelatorioListItem.vue)): mesmo card do item de lista, mas a área de texto é um `<button>` nativo (acessível por teclado) que **abre o arquivo numa nova aba**; ao lado, o ícone de baixar (`color="primary"`, `mdi-download-outline`) e, só para quem pode, a lixeira (`color="error"`). Enquanto o arquivo é buscado o item fica desabilitado e o ícone de baixar mostra o carregamento.
- **Card de arquivo com tipo** ([`DocumentoListItem.vue`](../src/components/documentos/DocumentoListItem.vue), atas e normas): o mesmo card que abre arquivo + ações, com um ícone à esquerda pelo tipo do arquivo (`mdi-file-pdf-box`, `mdi-file-image-outline`, `mdi-file-word-outline`, em `primary`). O clique abre PDF/imagem numa nova aba e **baixa** DOCX (que o navegador não renderiza); o ícone de baixar existe sempre.
- **Diálogo de envio de arquivo** (`DocumentosListView` + [`EnviarDocumentoForm.vue`](../src/components/documentos/EnviarDocumentoForm.vue)): mesma estrutura do diálogo de geração — título ("Enviar ata"), uma frase com o que é aceito e o limite, o erro do backend em `v-alert` tonal (o diálogo continua aberto) e `v-file-input` com o botão primário `block` no fim. Sem o ícone de clipe externo do `v-file-input` (`prepend-icon=""`): o ícone fica dentro do campo, como nos demais campos.
- **Diálogo de geração** (`RelatoriosListView`): o FAB "+" abre um `v-dialog` com título, uma frase curta do que acontece, o erro do backend (`v-alert` tonal, o diálogo continua aberto) e os seletores com o botão primário `block` no fim — mesma estrutura do diálogo de editar prédio. Opções que o backend recusaria (meses futuros) ficam desabilitadas, não escondidas.
- **Folha de filtros** ([`CompromissoFiltrosSheet.vue`](../src/components/compromissos/CompromissoFiltrosSheet.vue)): `v-bottom-sheet` aberto pelo ícone de filtro ao lado do campo de busca (um ponto azul indica filtro ativo). Mudanças valem na hora; "Limpar filtros" e "Ver resultados" no rodapé. Um prédio filtrado aparece também como `v-chip` fechável sob a busca.
- **Formulário longo com ação fixa** ([`CompromissoForm.vue`](../src/components/compromissos/CompromissoForm.vue)): quando o formulário rola no celular, o botão primário fica `sticky` no rodapé, sobre fundo sólido. Data e horário usam `<input type="date|time">` nativos (seletor do próprio celular), com `persistent-placeholder` para o rótulo não colidir com o valor.
- **`data-cy`**: todo elemento interativo relevante (inputs, botões, links, alertas) leva um `data-cy` estável, usado pelos testes Cypress. Ao redesenhar, **preserve os `data-cy` existentes** — mudar markup não deve quebrar teste. Ver [TEST.md](TEST.md).
- **`v-alert` (erros/avisos)**: o ícone da esquerda vem alinhado ao topo por padrão no Vuetify (`align-self: flex-start`) — fica descentralizado quando a mensagem quebra em mais de uma linha. Corrigido globalmente em [`src/assets/main.css`](../src/assets/main.css) (importado uma vez em `main.ts`), não tela a tela. O `main.css` é reservado para comportamento padrão do Vuetify sem prop equivalente e para as variáveis globais de layout (altura do cabeçalho, largura da coluna, rail e FAB — ver "Layout responsivo").

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
