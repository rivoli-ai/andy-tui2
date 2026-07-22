#!/usr/bin/env bash
#
# ci-graph-test.sh
#
# Single source of truth for the build+test graph that CI and the release
# workflow both execute. Running this locally reproduces exactly what CI does,
# so "local documented commands match CI commands".
#
# Responsibilities (see GitHub issue #27):
#   1. Optionally enforce a clean checkout (no pre-existing bin/obj).
#   2. Restore the whole solution.
#   3. Build the same complete graph that will be tested and packed.
#   4. Enumerate every test project, list it in the job output, and run it
#      only after confirming its binary was produced by this job.
#   5. Skip only projects on an explicit, tracked exclusion list.
#
# Usage:
#   scripts/ci-graph-test.sh [--configuration Debug|Release] [--require-clean]
#                            [--clean-check-only] [--no-test]
#
# Environment:
#   RUN_PARITY=true   Also run the Playwright browser parity suite (otherwise
#                     it is a tracked exclusion because it needs browsers).
#
set -euo pipefail

CONFIGURATION="Debug"
REQUIRE_CLEAN="false"
RUN_TESTS="true"
CLEAN_CHECK_ONLY="false"
SOLUTION="Andy.Tui.sln"
TFM="net8.0"

# Repository root = directory that contains the solution. Resolve relative to
# this script so the command works from any working directory.
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
cd "${REPO_ROOT}"

while [[ $# -gt 0 ]]; do
  case "$1" in
    --configuration|-c)
      CONFIGURATION="$2"; shift 2 ;;
    --require-clean)
      REQUIRE_CLEAN="true"; shift ;;
    --clean-check-only)
      CLEAN_CHECK_ONLY="true"; shift ;;
    --no-test)
      RUN_TESTS="false"; shift ;;
    *)
      echo "::error::Unknown argument: $1" >&2
      exit 2 ;;
  esac
done

# ---------------------------------------------------------------------------
# Tracked test-project exclusions. Every entry MUST have a reason. Anything not
# listed here is expected to build and run. This is the "explicit tracked
# exclusion" required by issue #27's acceptance criteria.
# ---------------------------------------------------------------------------
declare -a EXCLUSIONS=(
  "tests/Andy.Tui.Parity.Playwright|browser parity suite; needs Playwright browsers (set RUN_PARITY=true to include)"
  "tests/Andy.Tui.CliWidgets.Tests|not yet a member of the solution graph; tracked by #28 (solution membership) and #30 (CliWidgets references)"
)

is_excluded() {
  local proj_dir="$1"
  for entry in "${EXCLUSIONS[@]}"; do
    local path="${entry%%|*}"
    if [[ "${proj_dir}" == "${path}" ]]; then
      # Playwright is only excluded when parity is off.
      if [[ "${path}" == "tests/Andy.Tui.Parity.Playwright" && "${RUN_PARITY:-false}" == "true" ]]; then
        return 1
      fi
      return 0
    fi
  done
  return 1
}

exclusion_reason() {
  local proj_dir="$1"
  for entry in "${EXCLUSIONS[@]}"; do
    local path="${entry%%|*}"
    if [[ "${proj_dir}" == "${path}" ]]; then
      echo "${entry#*|}"
      return 0
    fi
  done
  echo "unknown"
}

echo "=============================================================="
echo " Andy.Tui project-graph build+test"
echo "   configuration : ${CONFIGURATION}"
echo "   require-clean : ${REQUIRE_CLEAN}"
echo "   check-only    : ${CLEAN_CHECK_ONLY}"
echo "   run-tests     : ${RUN_TESTS}"
echo "   RUN_PARITY    : ${RUN_PARITY:-false}"
echo "=============================================================="

# ---------------------------------------------------------------------------
# 1. Clean-checkout guard.
# ---------------------------------------------------------------------------
if [[ "${REQUIRE_CLEAN}" == "true" ]]; then
  echo "==> Verifying clean checkout (no pre-existing bin/obj)"
  # Scan the entire repository (including the root and benchmarks/) so any
  # pre-existing build output fails the guard, not just output under
  # src/tests/examples. Prune .git to avoid false positives from its internals.
  STALE="$(find . -type d -name .git -prune -o -type d \( -name bin -o -name obj \) -print 2>/dev/null || true)"
  if [[ -n "${STALE}" ]]; then
    echo "::error::Pre-existing build output found; CI must run from a clean checkout:" >&2
    echo "${STALE}" >&2
    exit 1
  fi
fi

if [[ "${CLEAN_CHECK_ONLY}" == "true" ]]; then
  if [[ "${REQUIRE_CLEAN}" != "true" ]]; then
    echo "::error::--clean-check-only requires --require-clean" >&2
    exit 2
  fi
  echo "==> Clean-check-only completed successfully"
  exit 0
fi

# ---------------------------------------------------------------------------
# 2. Restore the whole solution.
# ---------------------------------------------------------------------------
echo "==> Restoring ${SOLUTION}"
dotnet restore "${SOLUTION}"

# ---------------------------------------------------------------------------
# 3. Build the complete graph.
# ---------------------------------------------------------------------------
echo "==> Building ${SOLUTION} (${CONFIGURATION})"
dotnet build "${SOLUTION}" --configuration "${CONFIGURATION}" --no-restore

if [[ "${RUN_TESTS}" != "true" ]]; then
  echo "==> --no-test set; skipping test execution"
  exit 0
fi

# ---------------------------------------------------------------------------
# 4/5. Enumerate, list, verify binaries, run.
# ---------------------------------------------------------------------------
# Portable array fill (macOS ships bash 3.2 which lacks `mapfile`).
TEST_PROJECTS=()
while IFS= read -r line; do
  TEST_PROJECTS+=("${line}")
done < <(find tests -name '*.csproj' | sort)

echo ""
echo "==> Test project inventory (${#TEST_PROJECTS[@]} discovered)"

declare -a TO_RUN=()
declare -a FAILED=()
MISSING_BINARY="false"

for proj in "${TEST_PROJECTS[@]}"; do
  proj_dir="$(dirname "${proj}")"
  proj_name="$(basename "${proj}" .csproj)"
  assembly="${proj_dir}/bin/${CONFIGURATION}/${TFM}/${proj_name}.dll"

  if is_excluded "${proj_dir}"; then
    echo "  [EXCLUDED] ${proj_name} -- $(exclusion_reason "${proj_dir}")"
    continue
  fi

  if [[ -f "${assembly}" ]]; then
    echo "  [BUILT]    ${proj_name} -> ${assembly}"
    TO_RUN+=("${proj}")
  else
    echo "  [MISSING]  ${proj_name} -- expected binary ${assembly} was NOT produced by this build"
    MISSING_BINARY="true"
  fi
done

if [[ "${MISSING_BINARY}" == "true" ]]; then
  echo "::error::One or more test projects did not produce a binary. They must be added to the solution (#28) or given an explicit tracked exclusion." >&2
  exit 1
fi

echo ""
echo "==> Running ${#TO_RUN[@]} test project(s)"
for proj in "${TO_RUN[@]}"; do
  echo "---- dotnet test ${proj} (${CONFIGURATION}) ----"
  if ! dotnet test "${proj}" --configuration "${CONFIGURATION}" --no-build --verbosity normal; then
    FAILED+=("${proj}")
  fi
done

echo ""
if [[ ${#FAILED[@]} -gt 0 ]]; then
  echo "::error::${#FAILED[@]} test project(s) failed:" >&2
  for proj in "${FAILED[@]}"; do
    echo "  - ${proj}" >&2
  done
  exit 1
fi

echo "==> All ${#TO_RUN[@]} test project(s) passed for ${CONFIGURATION}."
