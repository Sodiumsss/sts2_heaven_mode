# HeavenMode

[中文](./README.md) | [English](./README.en.md)

## Introduction

**Heaven** introduces **10 brand-new difficulty levels** beyond the base game experience.  
Each level adds new challenges and twists, pushing players to adapt their strategy and endure increasingly punishing conditions within the Spire.

To unlock the first Heaven difficulty, you must **first complete the official Difficulty 10**.

If you prefer to unlock all difficulties immediately, you can do so manually:

1. Install the mod and **launch the game once**.
2. Open the mod folder.
3. Locate the configuration file and set the **`unlock` field to `true`**.

After that, all Heaven difficulties will be available.

## Developer Notes

Before running `tools/build_release.ps1`, make sure the `$godot` variable in the script points to a valid Godot executable, such as **MegaDot 4.5.1** (`https://megadot.megacrit.com/`), or replace it with any other available path on your machine.

### Reference Docs

The `docs/ref/` directory contains internal class reference notes gathered during development. Each file corresponds to a game class that the mod hooks into, and documents:

- Relevant method signatures and key vanilla logic
- The Harmony patch strategy used for each Heaven level (Prefix / Postfix) and the reasoning behind it
- Edge cases and implementation caveats

When adding a new Heaven level or modifying an existing mechanic, start by reading the relevant reference doc to understand the vanilla behavior before deciding where to patch.
