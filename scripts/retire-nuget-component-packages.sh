#!/usr/bin/env bash
#
# Retire the legacy Andy.Tui component package versions from nuget.org.
# The default mode is a read-only inventory. Pass --execute and provide
# NUGET_API_KEY to unlist every currently listed version.

set -euo pipefail

MODE="inventory"
if [[ "${1:-}" == "--execute" ]]; then
  MODE="execute"
elif [[ $# -ne 0 ]]; then
  echo "Usage: $0 [--execute]" >&2
  exit 2
fi

if ! command -v curl >/dev/null 2>&1 || ! command -v jq >/dev/null 2>&1; then
  echo "curl and jq are required." >&2
  exit 1
fi

if [[ "${MODE}" == "execute" && -z "${NUGET_API_KEY:-}" ]]; then
  echo "NUGET_API_KEY is required with --execute." >&2
  exit 1
fi

readonly REGISTRATION_BASE="https://api.nuget.org/v3/registration5-gz-semver2"
readonly NUGET_SOURCE="https://api.nuget.org/v3/index.json"
readonly -a PACKAGE_IDS=(
  "Andy.Tui.Animations"
  "Andy.Tui.Backend.Terminal"
  "Andy.Tui.CliWidgets"
  "Andy.Tui.Compose"
  "Andy.Tui.Compositor"
  "Andy.Tui.Core"
  "Andy.Tui.DisplayList"
  "Andy.Tui.Input"
  "Andy.Tui.Layout"
  "Andy.Tui.Observability"
  "Andy.Tui.Style"
  "Andy.Tui.Text"
  "Andy.Tui.Virtualization"
  "Andy.Tui.Widgets"
)

listed_versions() {
  local package_id="$1"
  local package_key
  local index_json
  local page_url

  package_key="$(printf '%s' "${package_id}" | tr '[:upper:]' '[:lower:]')"
  index_json="$(curl --fail --silent --show-error --compressed \
    "${REGISTRATION_BASE}/${package_key}/index.json")"

  jq -r '.items[] | .items[]? | select(.catalogEntry.listed != false) | .catalogEntry.version' \
    <<<"${index_json}"

  while IFS= read -r page_url; do
    [[ -z "${page_url}" ]] && continue
    curl --fail --silent --show-error --compressed "${page_url}" |
      jq -r '.items[] | select(.catalogEntry.listed != false) | .catalogEntry.version'
  done < <(jq -r '.items[] | select(.items == null) | ."@id"' <<<"${index_json}")
}

total=0
for package_id in "${PACKAGE_IDS[@]}"; do
  version_output="$(listed_versions "${package_id}")"
  versions=()
  while IFS= read -r version; do
    [[ -n "${version}" ]] && versions+=("${version}")
  done < <(printf '%s\n' "${version_output}" | sort)

  echo "${package_id}: ${#versions[@]} listed version(s)"
  for version in "${versions[@]}"; do
    echo "  - ${version}"
    if [[ "${MODE}" == "execute" ]]; then
      dotnet nuget delete "${package_id}" "${version}" \
        --source "${NUGET_SOURCE}" \
        --api-key "${NUGET_API_KEY}" \
        --non-interactive
    fi
  done
  total=$((total + ${#versions[@]}))
done

if [[ "${MODE}" == "inventory" ]]; then
  echo "Inventory complete: ${total} listed component version(s); no changes made."
  exit 0
fi

echo "Retirement requests complete: ${total} component version(s) unlisted."
