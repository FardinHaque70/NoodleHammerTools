# Noodle Hammer Tools

This UPM package is mirrored from the development source under `Assets/Noodle Hammer`.

After making code changes in the Unity project, run `upm/sync_from_assets.sh` to refresh this package mirror.

Standalone Unity editor tools packaged together under one `Noodle Hammer` UPM package.

Included tools:

- `Noodle Hammer Hierarchy`: smarter hierarchy icons, row styling, and settings.
- `Noodle Hammer Animator`: editor playback controls and Avatar T-pose utilities on `Animator` inspectors.
- `Noodle Hammer Transform`: quick reset, copy, and paste actions for `Transform` inspectors.

Each tool lives in its own editor assembly so it can be removed independently without breaking the others.

## Install via Git UPM

### Option 1: Unity Package Manager Git URL

In Unity:

1. Open `Window > Package Manager`
2. Click the `+` button
3. Choose `Add package from git URL...`
4. Paste:

```text
https://github.com/FardinHaque70/NoodleHammerTools.git?path=/upm/src
```

### Option 2: Edit `Packages/manifest.json`

Add this entry to your Unity project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.noodlehammer.tools": "https://github.com/FardinHaque70/NoodleHammerTools.git?path=/upm/src"
  }
}
```
