#!/bin/sh
#
# Writes the runtime configuration the Angular app reads at startup.
#
# This is what makes one image runnable in every environment. An Angular build inlines whatever it is
# given, so baking the API URL in would mean building a separate image per environment — and then the
# artefact that reaches production is not the one that was tested.
#
# nginx runs every executable in /docker-entrypoint.d before starting, so this needs no custom
# ENTRYPOINT and keeps the base image's own startup behaviour intact.

set -eu

API_BASE_URL="${API_BASE_URL:-http://localhost:5080}"
CONFIG_PATH=/usr/share/nginx/html/config.json

# Escaped before interpolation: the value comes from the environment, and an unescaped quote would
# produce a config.json the app cannot parse, failing at runtime rather than at deploy.
ESCAPED=$(printf '%s' "$API_BASE_URL" | sed 's/\/\\/g; s/"/\\"/g')

cat > "$CONFIG_PATH" <<JSON
{
  "apiBaseUrl": "${ESCAPED}"
}
JSON

echo "workboard: apiBaseUrl set to ${API_BASE_URL}"
