#!/bin/zsh
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
ASSETS_DIR="$ROOT_DIR/Assets/Noodle Hammer"
UPM_DIR="$ROOT_DIR/upm/src"

sync_tool() {
  local tool_name="$1"
  local source_dir="$ASSETS_DIR/$tool_name/Editor"
  local target_dir="$UPM_DIR/Editor/$tool_name/Editor"

  rm -rf "$target_dir"
  mkdir -p "$(dirname "$target_dir")"
  cp -R "$source_dir" "$target_dir"
}

sync_tool "Animator"
sync_tool "Hierarchy"
sync_tool "Transform"

echo "Synced Assets/Noodle Hammer tools into upm/src"
