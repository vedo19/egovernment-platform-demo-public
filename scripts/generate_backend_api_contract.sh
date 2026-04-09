#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
OUT_FILE="${1:-$ROOT_DIR/contracts/backend-api.snapshot}"

tmp_file="$(mktemp)"
trap 'rm -f "$tmp_file"' EXIT

{
  echo "# Backend API Contract Snapshot"
  echo
  echo "Generated from controller and gateway route declarations."
  echo
} > "$tmp_file"

find "$ROOT_DIR/src" -path "*/Controllers/*Controller.cs" | sort | while read -r file; do
  rel="${file#$ROOT_DIR/}"
  service="$(echo "$rel" | cut -d/ -f2)"
  controller_file="$(basename "$file")"
  controller_name="${controller_file%Controller.cs}"

  base_route="$(sed -n 's/^[[:space:]]*\[Route("\([^"]*\)")\].*/\1/p' "$file" | head -n1)"
  base_route="${base_route//\[controller\]/$controller_name}"

  echo "## ${service}/${controller_name}" >> "$tmp_file"
  echo >> "$tmp_file"

  awk -v base_route="$base_route" '
    {
      if ($0 ~ /^[[:space:]]*\[Http(Get|Post|Put|Delete|Patch)(\("[^"]*"\))?\]/) {
        method = ""
        route = ""
        if (match($0, /Http(Get|Post|Put|Delete|Patch)/, m)) {
          method = toupper(m[1])
        }
        if (match($0, /\("[^"]*"\)/, r)) {
          route = substr(r[0], 3, length(r[0]) - 4)
        }

        if (route == "") {
          full = base_route
        } else if (base_route == "") {
          full = route
        } else {
          full = base_route "/" route
        }

        gsub(/\/\//, "/", full)
        print "- " method " /" full
      }
    }
  ' "$file" | sort -u >> "$tmp_file"

  echo >> "$tmp_file"
done

{
  echo "## ApiGateway/Routes"
  echo
  sed -n 's/.*"UpstreamPathTemplate": "\([^"]*\)".*/- PATH \1/p' "$ROOT_DIR/src/ApiGateway/ocelot.json" | sort -u
  sed -n 's/.*MapGet("\([^"]*\)".*/- GET \1/p' "$ROOT_DIR/src/ApiGateway/Program.cs" | sort -u
  echo
} >> "$tmp_file"

mkdir -p "$(dirname "$OUT_FILE")"
mv "$tmp_file" "$OUT_FILE"
echo "Wrote contract snapshot to $OUT_FILE"
