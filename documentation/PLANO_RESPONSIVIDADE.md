# Plano de responsividade para tablet e desktop — euSíndico

O frontend foi construído mobile-first (RNF08) e funciona bem no celular. No desktop, a área logada vira uma coluna de 480px no meio da tela, com os FABs presos ao canto da janela, longe do conteúdo. Este plano adapta o desktop **sem alterar o fluxo mobile** e sem grandes refatorações.

Registrado em 2026-11-05.

## 1. Diagnóstico

| Problema | Causa |
|---|---|
| Coluna estreita no desktop | As 13 views logadas repetem `max-width: 480px` no CSS scoped |
| FAB longe do conteúdo | `position: fixed; right: 24px` prende o FAB ao canto da janela, não da coluna |
| Navegação lenta no desktop | Modelo "hub + voltar": trocar de módulo exige voltar à Home |
| Cards da Home empilhados | Coluna única mesmo com espaço sobrando |

Já respondem bem e não mudam: AuthLayout, Landing, diálogos (`max-width="400"`) e bottom sheets.

## 2. Modelo escolhido

Referência: layouts adaptativos do Material Design 3 (faixas compacta, média e expandida).

- **Largura:** 480px abaixo de 840px (breakpoint `md` do Vuetify); a partir dele, a largura disponível até 1200px.
- **Listas e hubs de cards em grade** de 2–3 colunas no desktop (classe `.app-grade`).
- **Formulários:** a tela ocupa a largura toda, mas o bloco de campos fica com no máximo 720px, centralizado (classe `.app-bloco-form`).
- **Navigation rail a partir de 840px:** marca no topo, Compromissos, Prédios e Configurações no meio, "Sair" no rodapé. Os módulos de um prédio continuam nos cards do detalhe do prédio.
- **Sem Home no desktop:** a tela inicial passa a ser Compromissos (redirecionamento no router e no `AppLayout`).
- **Convenções de desktop sem duplicar telas:** a mesma view troca só as peças de apresentação. A ação principal sai do FAB para o cabeçalho de página (título à esquerda, ação à direita), os cards viram tabela e a folha de filtros vira uma barra fixa ao lado da busca.
- **Descartado:** list-detail (lista e detalhe lado a lado, testado em Compromissos: as duas telas disputavam a atenção) e navigation drawer largo.

> **Revisões de 2026-11-05.** A primeira versão mantinha a Home no desktop e uma coluna única de 720px; na revisão visual a coluna ainda parecia "um celular dentro do monitor" e a Home ficou estranha ao lado do rail. Depois, o que ainda parecia de celular eram as convenções de interação (FAB, cards grandes, filtros escondidos), daí o cabeçalho de página, a tabela e a barra de filtros.

## 3. Passos

1. Variável `--app-largura-conteudo` em `main.css`; as views logadas usam a variável no lugar de `480px`.
2. FAB preso à coluna (fórmula considera a largura da coluna e a do rail, que vale 0 no celular).
3. Rail no `AppLayout`, só a partir de 840px. A lógica de sair vira o composable `useSair`, usado pela Home e pelo rail.
4. Home só no celular: guarda do router e `AppLayout` mandam `/home` para `/compromissos` no desktop.
5. AppHeader: no desktop vira cabeçalho de página (título à esquerda, slot `acoes` à direita); some o botão de Home e, nas telas de topo, o "voltar" (que iria para a Home).
6. Grade (`.app-grade`) nas listas e hubs de cards; bloco de formulário (`.app-bloco-form`) nas telas de formulário.
7. QR code embaixo do formulário de login e de cadastro, só no desktop. Cada um aponta para a própria tela (`/login` ou `/registrar`) a partir de `window.location.origin`.
8. Compromissos no desktop: ação no cabeçalho, tabela (`CompromissosTabela`) e barra de filtros (`CompromissoFiltrosBarra`), com as regras dos filtros compartilhadas com a folha do celular.
9. Testes: e2e atuais com viewport de celular (390×844); specs e2e de desktop para o rail e para Compromissos; testes unitários das regras compartilhadas.
10. Documentação: DESIGN.md e ARCHITECTURE.md (seção 5) do frontend.
11. Demais listas no desktop: ação no cabeçalho e tabela genérica (`AppTabela`) em Prédios, Planejamentos (com a faixa de orçamento na barra), Relatórios, Atas/Normas e Equipe; no detalhe do prédio, "Editar" e "Remover" no cabeçalho.
