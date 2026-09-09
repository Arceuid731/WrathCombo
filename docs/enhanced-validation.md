# Enhanced validation

## Automated

- Detached tests exercise configuration deltas, unknown fields, collection removals, typed legacy JSON, static job dictionaries, backups, lane selection, action matching and stale recommendations.
- The existing local Wrath configuration can be read by passing its path to the test executable. This check never writes to that file.
- A Release build compiles all job presets against Dalamud API 15.
- Package validation verifies the assembly/manifest identity and version, required dependencies, licenses and localized assemblies. It rejects the original plugin DLL/manifest and configuration backups.

## In-game acceptance (pending)

These require a running game with the plugin loaded; compilation cannot verify native UI geometry or combat decisions.

1. Disable Wrath Combo and enable Enhanced with an existing configuration. Check selected presets, custom actions and job-specific values. Change one option, disable Enhanced and verify that original Wrath reads it. Switch back and verify teaching settings persist.
2. Enable a single-target DPS preset and use a training dummy. Check a normal spell slot and a custom DPS button: red outlines must match the damage window throughout GCDs, weaves and level-synced action upgrades.
3. As WHM/SCH/AST/SGE, enable single-target and area healing presets. Test injured self, injured ally, several injured allies, active Regen/Excogitation, shields and full health. Check the displayed target, thresholds and a separate green outline. Test a heal that resolves to a self buff and a ground-target spell.
4. Verify no action or targeting is performed without a button/window click in Manual play. Test both a click-to-target name and an enabled click-to-use icon. Change target/job immediately before clicking: stale suggestions must be ignored.
5. Move and resize each unlocked window independently, including with click-through enabled. Lock both position and size, reload, and check the saved layouts at different Dalamud UI scales. Change icon size, opacity, colors and name/target/GCD visibility; test long names and their tooltips in narrow windows. Reset the layout and verify compact dimensions and starting positions. Use preview for empty windows, then disable it.
6. Test hotbar paging, hidden hotbars, HUD scales, the main cross hotbar and both double cross hotbars. Check alignment in fullscreen/windowed mode and after moving the game between monitors.
7. Confirm overlays clear on death, zoning, logout, mount, cutscenes and PvP. Disable/re-enable Enhanced repeatedly and check no duplicate callbacks or DTR entries remain.

## Design boundaries

Recommendations reuse enabled one-button presets. Their target context and retarget registrations are scoped to evaluation; they never publish retargets into the live button dictionary. Explicit icon clicks re-evaluate and require the same action/target before making one request through the native action manager. Already-resolved clicks bypass combo replacement and live retargeting for that call.

Wrath's original IPC prefix is retained to preserve existing integrations with one edition active. Enhanced refuses startup when original Wrath is loaded. It cannot prevent the original plugin being manually enabled afterward; keep only one edition active.

Configuration metadata is read with `TypeNameHandling.None`. Shared writes normalize assembly metadata back to the original identity, apply only session deltas, and refuse stale external updates. Enhanced-specific preferences never enter the shared file. Corrupt JSON is not replaced with defaults.
