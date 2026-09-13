# Enhanced validation

## Automated

- Detached tests exercise configuration deltas, unknown fields, collection removals, typed legacy JSON, static job dictionaries, backups, damage target fallback, four independent channels, action matching and stale recommendations. They also verify the compact layout migration, independent AoE preferences, resize persistence, card dimensions and pixel padding at 48/64/96px HUD scales.
- The existing local Wrath configuration can be read by passing its path to the test executable. This check never writes to that file.
- A Release build compiles all job presets against Dalamud API 15.
- Package validation verifies the assembly/manifest identity and version, required dependencies, licenses and localized assemblies. It rejects the original plugin DLL/manifest and configuration backups.
- Positional IPC regression checks run the production hint service and recommendation context with detached game/IPC doubles. Temporary teaching targets cannot publish, refresh or clear live hints; live notifications resume after nested evaluation. The test reproduces the failure with the unmodified upstream service.

## In-game acceptance (pending)

These require a running game with the plugin loaded; compilation cannot verify native UI geometry or combat decisions.

1. Disable Wrath Combo and enable Enhanced with an existing configuration. Check selected presets, custom actions and job-specific values. Change one option, disable Enhanced and verify that original Wrath reads it. Switch back and verify teaching settings persist.
2. Enable Dancer Simple single-target and use a training dummy with Auto-Rotation off, Use Wrath's rotation targeting on, and Tank Target selected while solo. Check Standard Step appears, then follow the dance and combat rotation. Repeat with rotation targeting off and with a tank in the party. Check a normal spell slot and a custom DPS button: red outlines must match the damage window throughout GCDs, weaves and level-synced action upgrades. Check Debug → Teaching mode diagnostics if either window is empty.
3. As WHM/SCH/AST/SGE, enable single-target and area healing presets. Test injured self, injured ally, several injured allies, active Regen/Excogitation, shields and full health. Check the displayed target, thresholds and a separate green outline. Test a heal that resolves to a self buff and a ground-target spell.
4. With native Auto-Rotation and other automatic features off, verify teaching performs no action or targeting without a button/window click. Test both a click-to-target name and an enabled click-to-use icon. Change target/job immediately before clicking: stale suggestions must be ignored. Verify the native Auto-Rotation checkbox and DTR toggle work without an additional teaching toggle.
5. Move and resize each unlocked window independently, including with click-through enabled. Lock both position and size, reload, and check the saved layouts at different Dalamud UI scales. Change icon size, opacity, colors and name/target/GCD visibility; test long names and their tooltips in narrow windows. Reset the layout and verify compact dimensions and starting positions. Use preview for empty windows, then disable it.
6. Test Glow and Outline with hotbar paging, hidden hotbars, HUD scales, the main cross hotbar and both double cross hotbars. Check glow intensity, padding from 0 to 20 pixels, pulse, custom colors and shared support actions. AoE must have a second outer frame; shared single-target/AoE spells must not stack identical glows. Check alignment in fullscreen/windowed mode and after moving the game between monitors.
7. Confirm overlays clear on death, zoning, logout, mount, cutscenes and PvP. Disable/re-enable Enhanced repeatedly and check no duplicate callbacks or DTR entries remain and that the glow texture is recreated correctly.
8. Enable both Dancer damage presets and both custom damage buttons on one dummy: both suggestions should appear, with an AoE badge and double frame only for AoE. Repeat with four healer custom buttons, including Astral Draw appearing in multiple rotations. Each custom button must use its own channel's highlight settings; each window click must re-evaluate the same channel. Disabling an AoE preset must clear only the corresponding guidance.
9. Upgrade from a two-window configuration: verify colors, position, click behavior and visibility survive. Check the one-time switch to compact dimensions, then resize all four windows, reload and confirm those sizes persist. Check icon-only, one-line and two-line cards at normal and enlarged UI scales.
10. With a positional IPC consumer and a melee single-target preset enabled, compare teaching on/off while its rotation targeting selects a different enemy. The consumer must follow the live rotation target. Check nested/area guidance does not clear or redirect its hint. Test the revised opener countdown blocking, cancellation and restart, especially DNC; check BST simple-mode guidance with its preset enabled and SGE Druochole targeting from a damage rotation.

## Design boundaries

Recommendations reuse enabled one-button presets. Their target context and retarget registrations are scoped to evaluation; they never publish retargets into the live button dictionary. Explicit icon clicks re-evaluate the same channel and require the same source/action/target before making one request through the native action manager. Already-resolved clicks bypass combo replacement and live retargeting for that call.

Wrath's original IPC prefix is retained to preserve existing integrations with one edition active. Enhanced refuses startup when original Wrath is loaded. It cannot prevent the original plugin being manually enabled afterward; keep only one edition active.

Configuration metadata is read with `TypeNameHandling.None`. Shared writes normalize assembly metadata back to the original identity, apply only session deltas, and refuse stale external updates. Enhanced-specific preferences never enter the shared file. Corrupt JSON is not replaced with defaults.
