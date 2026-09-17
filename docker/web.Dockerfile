# syntax=docker/dockerfile:1

# =================================================================================================
# WorkBoard web client
#
# An Angular build produces static files, so the runtime stage is nginx and nothing else. The image
# is around 21 MB, almost all of it nginx and Alpine.
# =================================================================================================

# ------------------------------------------------------------------------------------------ build
FROM node:24-alpine AS build
WORKDIR /src

# Manifests first, so `npm ci` is cached until a dependency actually changes. Angular installs are
# large and slow; this is the difference between a 10-second rebuild and a 90-second one.
COPY src/WorkBoard.Web/package.json src/WorkBoard.Web/package-lock.json ./

# `npm ci` rather than `npm install`: it installs exactly what the lock file pins and fails if the
# manifest and the lock file disagree. An image must not quietly pick up a newer transitive version
# than the one that was tested.
RUN npm ci --no-audit --no-fund

COPY src/WorkBoard.Web/ ./

RUN npm run build

# ---------------------------------------------------------------------------------------- runtime
FROM nginx:1.27-alpine AS runtime

# Replaces the default server block: SPA routing, compression and the security headers.
COPY docker/nginx.conf /etc/nginx/conf.d/default.conf

# Writes config.json from environment variables at container start, which is what lets one image run
# in every environment. See the entrypoint and RuntimeConfigService for the reasoning.
COPY docker/web-entrypoint.sh /docker-entrypoint.d/40-workboard-config.sh
RUN chmod +x /docker-entrypoint.d/40-workboard-config.sh

COPY --from=build /src/dist/WorkBoard.Web/browser /usr/share/nginx/html

EXPOSE 80

HEALTHCHECK --interval=15s --timeout=3s --start-period=5s --retries=3 \
    CMD ["wget", "--quiet", "--tries=1", "--spider", "http://localhost/healthz"]
