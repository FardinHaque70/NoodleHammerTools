# Noodle Hammer Tools

`Noodle Hammer Tools` is a collection of small Unity editor productivity tools bundled into a single Git UPM package.

This repo currently includes:

- `Improved Hierarchy`: adds clearer hierarchy visuals and editor-side hierarchy enhancements.
- `Animator Component Improvement`: improves the `Animator` inspector with extra editor utilities such as clip playback helpers.
- `Transform Buttons`: adds quick actions to the `Transform` inspector for common position, rotation, and scale workflows.

## Package Info

- Package name: `com.noodlehammer.tools`
- Unity version: `2022.3+`
- UPM source path: `upm/src`

## Install With Git UPM

Add this dependency to your Unity project's `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.noodlehammer.tools": "https://github.com/FardinHaque70/NoodleHammerTools.git?path=/upm/src"
  }
}
```

If you want to lock to a specific branch, tag, or commit, append a revision after the URL:

```json
{
  "dependencies": {
    "com.noodlehammer.tools": "https://github.com/FardinHaque70/NoodleHammerTools.git?path=/upm/src#main"
  }
}
```

After saving `manifest.json`, let Unity refresh packages and recompile scripts.

## Repo Layout

- `Assets/Noodle Hammer`: development source used inside the Unity project.
- `upm/src`: the mirrored package content that Unity installs through Git UPM.
- `upm/sync_from_assets.sh`: sync script for refreshing the UPM mirror from the in-project source.

## Updating The Package

When you make changes under `Assets/Noodle Hammer`, run:

```bash
./upm/sync_from_assets.sh
```

Then commit the updated files so the Git UPM package stays in sync with the development source.
