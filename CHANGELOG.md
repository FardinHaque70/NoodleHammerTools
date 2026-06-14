# Changelog

All notable changes to this repository will be documented in this file.

## 2026-06-14

### Fixed

- Restored the shared `Core` editor files to the Git UPM mirror so imported projects can resolve `NoodleHammer.Core.Editor`.
- Added `upm/src/CHANGELOG.md.meta` to avoid immutable package import warnings for the package changelog file.
- Updated `upm/sync_from_assets.sh` so future package syncs include the shared `Core` folder and its folder metadata.

## 2026-06-14

### Changed

- Added a root `README.md` describing the repo contents and Git UPM installation flow.
- Clarified that this repository currently ships three editor tools:
  - `Improved Hierarchy`
  - `Animator Component Improvement`
  - `Transform Buttons`

### Removed

- Removed the old `Preview Forge` source and documentation from this repository so `Noodle Hammer Tools` reflects the current toolset only.
