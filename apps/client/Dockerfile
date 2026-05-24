FROM node:22-alpine AS build
WORKDIR /workspace

COPY package.json package-lock.json nx.json tsconfig.base.json postcss.config.js tailwind.config.js eslint.config.mjs jest.config.ts jest.preset.js ./
COPY apps ./apps

RUN npm ci
RUN npx nx build client --configuration=production

FROM nginx:1.27-alpine

RUN apk add --no-cache gettext

COPY apps/client/nginx/default.conf /etc/nginx/conf.d/default.conf
COPY apps/client/docker/40-runtime-config.sh /docker-entrypoint.d/40-runtime-config.sh
RUN chmod +x /docker-entrypoint.d/40-runtime-config.sh

COPY --from=build /workspace/dist/apps/client/browser /usr/share/nginx/html
