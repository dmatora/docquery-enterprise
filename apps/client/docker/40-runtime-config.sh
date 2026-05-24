#!/bin/sh
set -eu

: "${DOCQUERY_API_BASE_URL:=http://localhost:5152}"
export DOCQUERY_API_BASE_URL

template_path="/usr/share/nginx/html/assets/runtime-config.template.json"
target_path="/usr/share/nginx/html/assets/runtime-config.json"

if [ ! -f "$template_path" ]; then
  echo "Runtime config template not found: $template_path" >&2
  exit 1
fi

envsubst '${DOCQUERY_API_BASE_URL}' < "$template_path" > "$target_path"
rm -f "$template_path"
