#!/usr/bin/env bash
#
# Installed-package smoke test.
#
# Packs the Andy.Tui meta-package to a temporary local NuGet feed, generates a throwaway console
# application that references ONLY that packed artifact (not the source projects), and builds and
# runs it. This proves the shipped .nupkg actually exposes a usable public surface — the full
# reactive-to-terminal pipeline (DisplayList -> Compositor -> AnsiEncoder) — to an external
# consumer, which the in-repo project-reference tests cannot demonstrate.
#
# Everything happens under a private temp directory with an isolated NUGET_PACKAGES cache, so the
# run leaves no hidden artifacts in the repo or the developer's global package cache and can run
# deterministically in CI. Exit status is non-zero on any failure.
#
# Usage: scripts/package-smoke-test.sh
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
CONFIG="${CONFIGURATION:-Release}"
PKG_VERSION="$(grep -oE '<Version>[^<]+</Version>' "${REPO_ROOT}/Directory.Build.props" | head -1 | sed -E 's/<\/?Version>//g')"

WORK="$(mktemp -d)"
FEED="${WORK}/feed"
APP="${WORK}/consumer"
export NUGET_PACKAGES="${WORK}/nuget-cache"
mkdir -p "${FEED}" "${APP}"

cleanup() { rm -rf "${WORK}"; }
trap cleanup EXIT

echo "==> Package version: ${PKG_VERSION}"
echo "==> Temp workspace:  ${WORK}"

# The meta-package embeds the built library DLLs, so build them first, then pack.
echo "==> Building libraries (${CONFIG})"
dotnet build "${REPO_ROOT}/src/Andy.Tui/Andy.Tui.csproj" -c "${CONFIG}" --nologo -v quiet

echo "==> Packing meta-package to local feed"
dotnet pack "${REPO_ROOT}/src/Andy.Tui/Andy.Tui.csproj" -c "${CONFIG}" --no-build --nologo -v quiet -o "${FEED}"

# A consumer that references the packed artifact and exercises the real pipeline end to end.
cat > "${APP}/Consumer.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RestoreSources>${FEED};https://api.nuget.org/v3/index.json</RestoreSources>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Andy.Tui" Version="${PKG_VERSION}" />
  </ItemGroup>
</Project>
EOF

cat > "${APP}/Program.cs" <<'EOF'
using Andy.Tui.Backend.Terminal;
using Andy.Tui.Compositor;
using Andy.Tui.DisplayList;

var b = new DisplayListBuilder();
b.PushClip(new ClipPush(0, 0, 12, 1));
b.DrawRect(new Rect(0, 0, 12, 1, new Rgb24(0, 0, 0)));
b.DrawText(new TextRun(0, 0, "PKGOK", new Rgb24(255, 255, 255), new Rgb24(0, 0, 0), CellAttrFlags.None));
b.Pop();

var comp = new TtyCompositor();
var cells = comp.Composite(b.Build(), (12, 1));
var dirty = comp.Damage(new CellGrid(12, 1), cells);
var runs = comp.RowRuns(cells, dirty);
var bytes = new AnsiEncoder().Encode(runs, new TerminalCapabilities { TrueColor = true, Palette256 = true });

// Print a marker only if the encoded frame contains the drawn text.
var s = System.Text.Encoding.UTF8.GetString(bytes.ToArray());
Console.WriteLine(s.Contains("PKGOK") ? "CONSUMER_OK" : "CONSUMER_FAIL");
EOF

echo "==> Restoring + building consumer against packed feed"
dotnet build "${APP}/Consumer.csproj" -c "${CONFIG}" --nologo -v quiet

echo "==> Running consumer"
OUTPUT="$(dotnet run --project "${APP}/Consumer.csproj" -c "${CONFIG}" --no-build)"
echo "    consumer output: ${OUTPUT}"

if [[ "${OUTPUT}" != *"CONSUMER_OK"* ]]; then
  echo "FAIL: consumer did not report success against the packed artifact" >&2
  exit 1
fi

echo "PASS: packed artifact consumed successfully"
