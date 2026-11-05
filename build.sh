#!/usr/bin/env bash
# 在 Linux 沙箱中编译并打包活字格插件
# 前置：把 8 个活字格 DLL 放到 XyLink/refs/ 目录
set -euo pipefail

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
export PATH="/opt/dotnet:$PATH"
export DOTNET_ROOT="/opt/dotnet"

PROJ="$ROOT/XyLink/XyLink.csproj"
OUT="$ROOT/XyLink/bin"
TOOL="$ROOT/PackageTools/ForguncyPluginPackageTool.dll"

echo "==> 检查引用 DLL"
REFS_DIR="$ROOT/XyLink/refs"
required=(
  "Forguncy.Log.Abstractions.dll"
  "Forguncy.Commands.dll"
  "Forguncy.Commands.Design.dll"
  "GrapeCity.Forguncy.CellTypes.dll"
  "GrapeCity.Forguncy.CellTypes.Design.dll"
  "GrapeCity.Forguncy.Plugin.dll"
  "GrapeCity.Forguncy.Plugin.Design.dll"
  "GrapeCity.Forguncy.ServerApi.dll"
)
missing=0
for dll in "${required[@]}"; do
  if [[ ! -f "$REFS_DIR/$dll" ]]; then
    echo "  缺少: $dll"
    missing=1
  fi
done
if [[ $missing -ne 0 ]]; then
  echo "请把上述 DLL 放入 $REFS_DIR 后重试。"
  exit 1
fi

echo "==> 还原并编译 Release"
dotnet build "$PROJ" -c Release -p:EnableWindowsTargeting=true

echo "==> 调用打包工具"
dotnet "$TOOL" "$ROOT/XyLink/." "$ROOT/XyLink/bin/XyLink.dll" Release

echo "==> 完成"
find "$ROOT/XyLink/bin" -maxdepth 2 -name "*.zip" -o -name "*.forguncy" 2>/dev/null || true
