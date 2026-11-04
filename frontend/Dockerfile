# Imagem de produção do frontend: build estático do Vite servido pelo Caddy (ver deploy/README.md).
# Contexto de build: a pasta frontend/. O Caddyfile não entra na imagem — o compose monta deploy/Caddyfile.

FROM node:22-alpine AS build
WORKDIR /app

# O "prepare" do package.json roda "cypress install"; o binário do Cypress (centenas de MB) não é usado no build.
ENV CYPRESS_INSTALL_BINARY=0

COPY package.json package-lock.json ./
RUN npm ci

COPY . .

# URL pública da API, embutida no bundle no momento do build (o Vite não lê variáveis em tempo de execução).
ARG VITE_API_BASE_URL
RUN test -n "$VITE_API_BASE_URL" || (echo "VITE_API_BASE_URL é obrigatório" && exit 1)
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

RUN npm run build

FROM caddy:2.10-alpine AS final
COPY --from=build /app/dist /srv
