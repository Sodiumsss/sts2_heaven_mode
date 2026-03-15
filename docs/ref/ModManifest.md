# ModManifest

Namespace: `MegaCrit.Sts2.Core.Modding`

## Legacy structure

Observed in:

- `sts2_decompile/sts2/MegaCrit/sts2/Core/Modding/ModManifest.cs`

Fields on the current vanilla manifest model:

- `pck_name`
- `name`
- `author`
- `description`
- `version`

## Beta structure

Observed in:

- `sts2_decompile/sts2_beta/sts2/MegaCrit/sts2/Core/Modding/ModManifest.cs`

Fields on the newer vanilla manifest model:

- `id`
- `name`
- `author`
- `description`
- `version`
- `has_pck`
- `has_dll`
- `dependencies`
- `affects_gameplay`

## Packaging note for this repo

Newer vanilla now expects a manifest JSON outside the PCK named:

- `<mod_id>.json`

For this repo, the build keeps the legacy compatibility file:

- `mod_manifest.json`

And additionally generates the new manifest file:

- `HeavenMode.json`

The build script source of truth remains:

- `mod_manifest.json`

`tools/build_release.ps1` now:

- copies the legacy `mod_manifest.json` into the release folder unchanged for compatibility
- emits `HeavenMode.json` beside the DLL and PCK using the newer beta `ModManifest` shape
- uses `pck_name` from the legacy manifest as the new manifest `id`
