#!/bin/zsh
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
ASSETS_DIR="$ROOT_DIR/Assets/Noodle Hammer"
UPM_DIR="$ROOT_DIR/upm/src"

sync_tool() {
  local tool_name="$1"
  local source_dir="$ASSETS_DIR/$tool_name/Editor"
  local source_root_meta="$ASSETS_DIR/$tool_name.meta"
  local source_editor_meta="$ASSETS_DIR/$tool_name/Editor.meta"
  local target_root_dir="$UPM_DIR/Editor/$tool_name"
  local target_dir="$UPM_DIR/Editor/$tool_name/Editor"

  rm -rf "$target_root_dir"
  mkdir -p "$target_root_dir"
  cp -R "$source_dir" "$target_dir"

  if [[ -f "$source_root_meta" ]]; then
    cp "$source_root_meta" "$UPM_DIR/Editor/$tool_name.meta"
  fi

  if [[ -f "$source_editor_meta" ]]; then
    cp "$source_editor_meta" "$target_root_dir/Editor.meta"
  fi
}

sync_tool "Core"
sync_tool "Animator"
sync_tool "Hierarchy"
sync_tool "Transform"

echo "Synced Assets/Noodle Hammer tools into upm/src"
