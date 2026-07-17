#!/usr/bin/env bash
#
# validate-solution.sh
#
# Ensures every repository project (*.csproj under src/, tests/, examples/) is
# deliberately accounted for: it must either be a member of Andy.Tui.sln or be
# listed in scripts/solution-excluded-projects.txt. The script also flags stale
# solution entries that point at project files which no longer exist.
#
# Exit codes:
#   0  every project is either included in the solution or explicitly excluded
#   1  a project is missing from the solution (and not explicitly excluded),
#      the solution references a non-existent project, or the solution is invalid
#
# Run from the repository root:
#   ./scripts/validate-solution.sh
set -euo pipefail

repo_root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$repo_root"

solution="Andy.Tui.sln"
exclude_file="scripts/solution-excluded-projects.txt"

if [[ ! -f "$solution" ]]; then
  echo "error: $solution not found in $repo_root" >&2
  exit 1
fi

# Fail fast if the solution itself cannot be parsed (e.g. duplicate projects).
if ! sln_output="$(dotnet sln "$solution" list 2>&1)"; then
  echo "error: 'dotnet sln $solution list' failed:" >&2
  echo "$sln_output" >&2
  exit 1
fi

# Normalise the solution's project paths (strip the header lines, convert any
# backslashes to forward slashes so the comparison is OS independent).
solution_projects="$(printf '%s\n' "$sln_output" \
  | grep -E '\.csproj$' \
  | tr '\\' '/' \
  | sort -u)"

# Every project file that physically exists in the repository.
repo_projects="$(find src tests examples -name '*.csproj' 2>/dev/null | sed 's#^\./##' | sort -u)"

# Explicitly excluded projects (blank lines and '#' comments are ignored).
if [[ -f "$exclude_file" ]]; then
  excluded_projects="$(grep -vE '^\s*(#.*)?$' "$exclude_file" | tr '\\' '/' | sort -u || true)"
else
  excluded_projects=""
fi

status=0

# 1) Projects on disk that are neither in the solution nor explicitly excluded.
while IFS= read -r proj; do
  [[ -z "$proj" ]] && continue
  if ! grep -Fxq "$proj" <<<"$solution_projects" && ! grep -Fxq "$proj" <<<"$excluded_projects"; then
    echo "error: project not in solution and not explicitly excluded: $proj" >&2
    echo "       add it with: dotnet sln $solution add \"$proj\"" >&2
    echo "       or record the deliberate exclusion in $exclude_file" >&2
    status=1
  fi
done <<<"$repo_projects"

# 2) Solution entries that reference project files which no longer exist.
while IFS= read -r proj; do
  [[ -z "$proj" ]] && continue
  if [[ ! -f "$proj" ]]; then
    echo "error: solution references a missing project file: $proj" >&2
    status=1
  fi
done <<<"$solution_projects"

# 3) A project may not be both included and explicitly excluded.
while IFS= read -r proj; do
  [[ -z "$proj" ]] && continue
  if grep -Fxq "$proj" <<<"$solution_projects"; then
    echo "error: project is in the solution but also listed as excluded: $proj" >&2
    status=1
  fi
done <<<"$excluded_projects"

if [[ "$status" -eq 0 ]]; then
  count="$(printf '%s\n' "$solution_projects" | grep -c '\.csproj$' || true)"
  echo "solution validation passed: $count project(s) included, every repository project accounted for."
fi

exit "$status"
