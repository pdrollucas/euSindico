# Segurança — Frontend euSíndico

Este documento reúne as medidas de segurança do frontend do euSíndico. Complementa o [AUTHENTICATION.md](AUTHENTICATION.md) deste projeto (que descreve *onde e como o token vive* — armazenamento, renovação, logout), o [SECURITY.md do backend](../../backend/documentation/SECURITY.md) (que descreve tokens, senhas e tratamento de erro do lado da API) e a seção 6 do [RFC](../../documentation/RFC/RFC.md) (que descreve as *intenções* de segurança do projeto).

> O euSíndico é um produto real, não só um exercício acadêmico — as decisões abaixo são avaliadas com esse padrão, o mesmo princípio já adotado no SECURITY.md do backend.

## Sumário

1. [Autenticação: risco e mitigação](#1-autenticação-risco-e-mitigação)
2. [Proteção contra XSS](#2-proteção-contra-xss)
3. [CSRF — por que não se aplica hoje](#3-csrf--por-que-não-se-aplica-hoje)
4. [Validação de entrada no cliente](#4-validação-de-entrada-no-cliente)
5. [Upload de arquivos (RN12)](#5-upload-de-arquivos-rn12)
6. [HTTPS](#6-https)
7. [CORS](#7-cors)
8. [Variáveis de ambiente](#8-variáveis-de-ambiente)
9. [Dependências de terceiros](#9-dependências-de-terceiros)
10. [Mapeamento com a seção 6.1 do RFC (OWASP Top 10)](#10-mapeamento-com-a-seção-61-do-rfc-owasp-top-10)
11. [Pendências conhecidas](#11-pendências-conhecidas)

## 1. Autenticação: risco e mitigação

O fluxo completo — onde cada token fica, renovação automática, logout — está centralizado em **[AUTHENTICATION.md](AUTHENTICATION.md)**. Esta seção cobre só o raciocínio de risco por trás das escolhas de lá.

**Refresh token em cookie `HttpOnly`:** desde a migração no backend, o refresh token nunca é acessível a JavaScript — um XSS bem-sucedido no frontend não consegue mais lê-lo (diferente do desenho anterior, com o token em `localStorage`). Essa é a mitigação estrutural, não só uma dependência de "impedir o XSS em si". A análise completa dos atributos do cookie (`HttpOnly`, `Secure`, `SameSite=None`, `Path=/auth`) está em [SECURITY.md do backend](../../backend/documentation/SECURITY.md), seção 1 — o frontend só precisa saber que existe e enviar `withCredentials: true` em toda chamada Axios para que ele trafegue.

**Access token só em memória, nunca persistido:** reduz a janela de exposição do token mais "poderoso" (o que autentica toda requisição) aos, no máximo, ~30 minutos em que ele é válido e só enquanto a aba está aberta — em vez das 8h totais da sessão, que é o tempo que o refresh token (mais restrito em uso, e agora inacessível a JS) fica exposto.

**Logout apaga os tokens independente do resultado da chamada ao backend** (o erro é capturado e nunca propaga — ver [AUTHENTICATION.md](AUTHENTICATION.md), seção 6) — decisão de segurança, não só de UX: o usuário espera sair do dispositivo mesmo que a chamada falhe por rede/timeout.

## 2. Proteção contra XSS

A defesa primária, conforme já definido no [RFC](../../documentation/RFC/RFC.md) (seção 6.1) e reafirmado no [SECURITY.md do backend](../../backend/documentation/SECURITY.md) (seção 3): o Vue.js escapa por padrão todo conteúdo interpolado (`{{ }}`), tratando-o sempre como texto.

Regras adicionais adotadas neste projeto:

- **`v-html` é proibido por padrão** — nenhuma tela deste projeto injeta HTML dinâmico vindo do backend ou de input do usuário. Se um caso futuro realmente precisar (ex: renderizar um trecho formatado), o conteúdo deve passar por uma biblioteca de sanitização (ex: DOMPurify) antes, nunca ser injetado cru.
- Nenhum uso de `eval`, `new Function`, ou atribuição dinâmica de `innerHTML` fora do `v-html` controlado acima.
- O ESLint do projeto (ver [QUALITY.md](QUALITY.md)) inclui a regra `vue/no-v-html` para pegar isso em code review automaticamente, não só por convenção.
- A camada de validação de entrada do backend (`NomeValidator`/`EmailValidator`, [SECURITY.md](../../backend/documentation/SECURITY.md#3-validação-de-entrada-e-proteção-contra-scripts-maliciosos-xss) seção 3) já rejeita payloads como `<script>...</script>` antes de chegarem a ser persistidos — o frontend não precisa (e não deve) tentar reimplementar essa sanitização, só evitar reabrir o problema ao renderizar dado bruto como HTML.

Essa proteção conversa com a análise de risco da seção 1: como o refresh token agora vive em cookie `HttpOnly` (inacessível a JavaScript), um XSS não o alcança de forma alguma. O escape padrão do Vue segue essencial — protege o access token em memória e a integridade da interface — e vale como defesa em profundidade.

## 3. CSRF — mitigado via CORS + preflight, não via SameSite

Diferente de uma aplicação puramente `Authorization: Bearer <token>`, o cookie `HttpOnly` do refresh token (seção 1) é enviado automaticamente pelo navegador em requisições para `/auth/refresh` e `/auth/logout` — isso reabre a superfície de CSRF nesses dois endpoints especificamente, já que `SameSite=None` (necessário porque frontend e backend ficam em domínios diferentes, ver [AUTHENTICATION.md](AUTHENTICATION.md), seção 3) não bloqueia o envio cross-site do cookie por si só.

A mitigação real, replicando o raciocínio do [SECURITY.md do backend](../../backend/documentation/SECURITY.md), seção 1: o CORS do backend nunca usa `AllowAnyOrigin` (sempre origens explícitas, ver seção 7 abaixo) e a API só aceita `Content-Type: application/json` — isso torna toda requisição de escrita a `/auth/*` uma requisição *não-simples*, que exige um *preflight* (`OPTIONS`) aprovado pelo CORS antes do navegador sequer enviar a requisição real com o cookie. Um site de origem não cadastrada não passa do preflight, então o cookie nunca chega a ser enviado numa tentativa de CSRF vinda de fora das origens liberadas. O frontend não precisa implementar nada adicional para isso (nenhum token CSRF customizado) — a defesa vive inteiramente na configuração de CORS do backend.

Para o resto da API (`/predios`, `/compromissos` etc.), a autenticação continua via header `Authorization: Bearer <token>`, que não é enviado automaticamente pelo navegador — sem cookie ambiente nesses endpoints, não há CSRF a mitigar ali.

## 4. Validação de entrada no cliente

Regras de validação (formato de e-mail, força de senha, caracteres aceitos no nome) são replicadas no frontend a partir das mesmas regras já implementadas no backend ([SECURITY.md](../../backend/documentation/SECURITY.md), seção 3, e [AUTHENTICATION.md do backend](../../backend/documentation/AUTHENTICATION.md), seção "Validações de entrada"), implementadas com **VeeValidate + Zod** ([ARCHITECTURE.md](ARCHITECTURE.md)) — o mesmo schema Zod usado para validar a *resposta* de um endpoint também descreve a *entrada* esperada de um formulário, evitando duas fontes de verdade divergentes para o formato do mesmo DTO:

| Campo | Regra (mesma do backend) |
|---|---|
| Nome | `^[\p{L}\s'-]+$` — só letras (com acentuação), espaços, hífen e apóstrofo |
| E-mail | Formato de e-mail válido, com domínio (padrão HTML5 `<input type="email">`) |
| Senha | Mínimo 8 caracteres, com maiúscula, minúscula, número e caractere especial (RNF04) |

**Isso é UX, não segurança.** A validação client-side (seja o schema Zod ou a checagem de tipo do TypeScript) existe só para dar feedback imediato (sem esperar um round-trip à API) — ela **nunca** é a barreira real. O backend revalida tudo de forma independente e é a única fonte de verdade; um cliente que contorne a validação do frontend (ex: chamando a API diretamente) esbarra exatamente nas mesmas regras do lado do servidor.

**Mesma lógica no cooldown de reenvio de código (RF06-A):** a tela de recuperação de senha desabilita o botão de enviar/reenviar código por 5 minutos localmente (contagem regressiva no rótulo do botão), espelhando o cooldown por conta que o backend aplica em `POST /auth/esqueci-senha` (RN15 / [SECURITY.md do backend](../../backend/documentation/SECURITY.md), seção 10). É UX — desestimula cliques repetidos e deixa claro o tempo de espera — **não** a barreira: o backend garante o cooldown mesmo que o front seja contornado (responde `204 No Content` sem gerar nem enviar nada durante a janela, de forma indistinguível de um e-mail não cadastrado — anti-enumeração). O valor (5 min) vive em `COOLDOWN_REENVIO_MS` ([`stores/recuperacaoSenhaStore.ts`](../src/stores/recuperacaoSenhaStore.ts)); o estado do fluxo entre as três telas fica nessa mesma store, nunca na URL, para não expor e-mail/código (ver [ARCHITECTURE.md](ARCHITECTURE.md), seção 4). O e-mail e o timestamp do cooldown são espelhados em `sessionStorage` para o timer sobreviver a um F5 (sem isso, recarregar zeraria a contagem e permitiria um reenvio que o backend aceitaria sem enviar) — mas o **código de redefinição nunca é persistido**: é um segredo de uso único, e guardá-lo em storage acessível a JavaScript teria o mesmo risco que motivou pôr o refresh token em cookie `HttpOnly` (seção 1).

**Cópia condicional (anti-enumeração na própria UI):** as telas de recuperação **nunca** afirmam que o e-mail existe — a cópia é sempre condicional ("se houver uma conta com esse e-mail, enviaremos/enviamos um código"). Como o front avança para a tela de código mesmo quando o e-mail não está cadastrado (o backend responde `204` idêntico nos dois casos), uma mensagem como "enviamos um código para X" revelaria, na prática, que X está cadastrado. Manter a cópia condicional estende o anti-enumeração do backend até a interface.

## 5. Upload de arquivos (RN12)

O RFC (RN12) exige arquivos PDF, DOCX, XLSX, JPG ou PNG, com no máximo 20 MB. O frontend valida extensão/tipo MIME e tamanho **antes** de iniciar o upload — evita gastar banda e dar feedback mais rápido ao síndico. Como na seção 4, essa é uma validação de UX: o backend ([SECURITY.md](../../backend/documentation/SECURITY.md), seção "Segurança dos Arquivos") é quem garante de fato que nada fora dessas regras chega a ser armazenado no AWS S3.

## 6. HTTPS

- **Produção:** AWS Amplify Hosting força HTTPS automaticamente (certificado gerenciado, redirecionamento de HTTP para HTTPS).
- **Desenvolvimento local:** o backend roda em `https://localhost:7091` (ver [GETTING_STARTED.md do backend](../../backend/documentation/GETTING_STARTED.md)). O dev server do Vite roda em HTTP por padrão (`http://localhost:5173`) — isso é aceitável só em localhost (não é conteúdo misto real para o navegador, que trata `localhost` como *secure context*); em produção, frontend e backend estarão ambos em HTTPS.

## 7. CORS

O backend já tem CORS configurado (`Program.cs`, policy `"Frontend"`, alimentada por `Cors:AllowedOrigins` — nunca `AllowAnyOrigin`, ver [SECURITY.md do backend](../../backend/documentation/SECURITY.md), seção 8). Em desenvolvimento, `http://localhost:5173` (porta padrão do Vite) já está liberado por padrão. **Falta só a origem de produção** (domínio do AWS Amplify Hosting) — pendência registrada na seção 11, já que esse domínio só é conhecido no momento do primeiro deploy real. `AllowCredentials()` **já está habilitado** no backend — necessário para o cookie `HttpOnly` do refresh token trafegar cross-origin (seção 1); por isso o frontend precisa enviar `withCredentials: true` em toda chamada Axios, não só nas de `/auth/*`.

## 8. Variáveis de ambiente

- Toda variável exposta ao Vite precisa do prefixo `VITE_` para ser embutida no build (ex: `VITE_API_BASE_URL`) — e isso significa que ela **fica visível no bundle JavaScript final**, acessível a qualquer um que abra o DevTools. Por definição, **nenhum segredo pode viver aqui** — só configuração pública (URLs, feature flags não sensíveis).
- Não existe (nem faria sentido existir) um equivalente frontend ao User Secrets do backend — o frontend não tem segredos de servidor para guardar. As únicas credenciais sensíveis do projeto (connection string, `Jwt:SecretKey`, SMTP) já vivem exclusivamente no backend.
- `.env.example` é versionado (documenta quais variáveis existem); `.env.local` (valores reais de cada ambiente) nunca é.

## 9. Dependências de terceiros

- `npm audit` roda como parte do CI (ver [QUALITY.md](QUALITY.md)) — falha o build em vulnerabilidades de severidade alta/crítica em dependências.
- `package-lock.json` é sempre versionado — builds reprodutíveis, sem "funciona na minha máquina" por causa de uma versão de dependência diferente.

## 10. Mapeamento com a seção 6.1 do RFC (OWASP Top 10)

| Medida do RFC | Onde está implementada (frontend) |
|---|---|
| Proteção contra XSS | Escape padrão do Vue + proibição de `v-html` cru (seção 2) |
| Autenticação segura | Access token em memória, refresh token com renovação automática — ver [AUTHENTICATION.md](AUTHENTICATION.md) e a análise de risco na seção 1 |
| Validação de entrada de dados | Réplica das regras do backend, só como UX (seção 4) |
| HTTPS | Amplify Hosting força HTTPS em produção (seção 6) |
| Tratamento de erros sem exposição de dados sensíveis | UI nunca expõe stack trace/detalhe técnico; erros mapeados por `status` HTTP (ver [ARCHITECTURE.md](ARCHITECTURE.md), seção 6) |

## 11. Pendências conhecidas

**Bloqueante antes de produção:**
- Adicionar a origem de produção do frontend (domínio do Amplify) a `Cors:AllowedOrigins` no backend — hoje só `http://localhost:5173` está liberado (seção 7). Em desenvolvimento local não bloqueia nada.
- Se frontend (Amplify) e backend (App Runner) ficarem em domínios totalmente diferentes em produção (não subdomínios de um domínio próprio compartilhado), o cookie do refresh token depende de `SameSite=None` (já configurado, ver seção 1) para continuar funcionando cross-site — isso já é o caso hoje e não é uma pendência de código, só um lembrete de que um domínio próprio compartilhado (`app.eusindico.com.br` / `api.eusindico.com.br`) permitiria endurecer para `SameSite=Strict`/`Lax` no futuro, caso vire prioridade.

**Podem esperar o gatilho correspondente:**
- Content-Security-Policy e demais security headers (`X-Frame-Options`/`frame-ancestors`, `X-Content-Type-Options`) configurados via AWS Amplify Hosting — relevante só a partir do primeiro deploy real, mesmo espírito da pendência de hospedagem já registrada no [ARCHITECTURE.md](ARCHITECTURE.md).

*(Histórico: a migração do refresh token de `localStorage` para cookie `httpOnly` + `Secure` + `SameSite=None`, antes listada aqui como pendência futura, já foi implementada no backend — ver [AUTHENTICATION.md](AUTHENTICATION.md), seção 3, e [SECURITY.md do backend](../../backend/documentation/SECURITY.md), seção 1.)*
