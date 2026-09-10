# Wrath Combo Enhanced

A personal fork of [Wrath Combo](https://github.com/PunishXIV/WrathCombo), with separate damage and healing guidance inspired by Rotation Solver Reborn's teaching mode.

## Install

Add this custom repository in Dalamud Settings → Experimental → Custom Plugin Repositories:

```text
https://raw.githubusercontent.com/Arceuid731/WrathCombo/main/repo.json
```

Install **Wrath Combo Enhanced**. Both editions can stay installed; enable only one at a time. Open Enhanced with `/wce` or `/wrathenhanced`.

## Teaching mode

1. Enable your job's damage and healing presets in PvE Features. Auto-action checkboxes are not required.
2. Open **Settings → Teaching mode**. The teaching interface uses English, with simple tooltips for every setting.
3. Customize four channels: **Damage — Single target**, **Damage — AoE**, **Healing — Single target** and **Healing — AoE**. Each has its own highlight, color, window and click settings.
4. Use **Preview windows** to arrange them. Turn off **Lock window**, then drag a corner to resize. Each window saves its own position and size.

**Highlight style → Glow** uses the game's soft luminous action frame, tinted with your Damage and Healing colors. **Highlight padding (px)** expands it on each side of the icon, with 3 pixels added by default. Adjust **Glow intensity** and **Pulse highlights**, or choose **Outline** for the plain border. AoE suggestions have a second outer frame. The frame follows each hotbar's size, including cross hotbars. Shared native spells use one glow per role with an AoE marker when applicable; custom buttons only highlight for their own channel.

Each compact window has an icon beside the action and target names, a thin color accent, and an optional GCD bar. AoE icons have a small **AoE** badge. There is no title. Icons shrink to fit smaller windows; long names are shortened and available in full on hover. Turn off both names for an icon-only layout, then reset or resize the window. Target names are green when already selected and orange when another target is suggested. Click a target name to select it. **Click icon to use action** is optional and off by default. **Click through when locked** passes clicks to the game while locked; unlocked windows remain interactive for arranging the layout.

On the first update to the four-window layout, existing windows adopt compact sizes while keeping their positions, colors and interaction settings. AoE windows inherit their role's settings and start below the single-target windows. Later manual resizing remains saved.

Use your normal hotbars, Wrath's custom one-button actions, or explicitly enable window clicks. Automatic execution is controlled by Wrath's native **Auto-Rotation** settings and IPC integrations. Teaching mode works with Auto-Rotation off or on.

All four recommendations run independently and require the matching enabled job preset. Damage AoE can appear alongside single-target damage even against one enemy, like the custom buttons. Healing still follows Wrath's single-target and group healing thresholds, including shields, regeneration and Excogitation. With Auto-Rotation off, guidance uses your selected enemy and normal healing target priorities. **Use Wrath's rotation targeting** follows the configured target modes only while Auto-Rotation is enabled. If that mode has no valid damage target, guidance falls back to your selected enemy.

If a window stays empty, **Debug → Teaching mode diagnostics** shows the latest evaluation. Changes in the reason for idle guidance are also recorded in the Dalamud log.

Teaching mode currently covers PvE presets marked as full damage/healing rotations. Existing PvP and utility features remain available on hotbars. Item/pomander proxies are not displayed as spell recommendations. It does not add a separate rotation pack or rewrite Wrath's job priorities.

Highlight colors identify the rotation that suggested an action, rather than a fixed spell category. Shared support actions such as Astrologian's Astral Draw can appear in either channel. Damage guidance needs an enemy target; healing guidance is evaluated when healing or cleansing is needed.

## Configuration sharing

Rotation settings are shared by default. Enhanced reads `pluginConfigs/WrathCombo.json` when loaded, stores its own configuration, and exports only the settings changed during that session. Disable the original plugin before enabling Enhanced; switch back by disabling Enhanced first.

Colors, positions and teaching preferences live separately in `pluginConfigs/WrathComboEnhanced/TeachingMode.json`. Unknown original settings are preserved. Enhanced creates a first-use backup (`WrathCombo.json.before-enhanced.bak`) and a rolling backup (`WrathCombo.json.enhanced.bak`). If the shared file changes externally during the session, exporting stops until Enhanced is reloaded.

## Development

Requires .NET 10 and Dalamud API 15 assemblies under `%APPDATA%/XIVLauncher/addon/Hooks/dev`.

```powershell
git submodule update --init --recursive
dotnet run --project Enhanced.Tests/Enhanced.Tests.csproj -c Release
dotnet build WrathCombo/WrathCombo.csproj -c Release --output artifacts/build
./.github/scripts/validate-package.ps1
```

Pushes to `main` build and test the plugin. A new project version publishes an `enhanced-vVERSION` GitHub release and updates `repo.json`. Release archives keep the `WrathComboEnhanced` identity, including translated resources. The original `WrathCombo` IPC provider is retained for compatibility when only Enhanced is enabled; action-request IPC uses `WrathComboEnhanced.ActionRequest`.

See [the validation checklist](docs/enhanced-validation.md) for live-game checks that cannot run in the detached test host.

## Credits

Wrath Combo and its rotations are maintained upstream by Team Wrath / Puni.sh and the contributors listed in the repository history. Their BSD 3-Clause license is retained in [LICENSE](LICENSE). [Rotation Solver Reborn](https://github.com/FFXIV-CombatReborn/RotationSolverReborn) supplied the UX reference; the Enhanced renderer and windows are implemented independently. Original project documentation is preserved in [docs/upstream-readme.md](docs/upstream-readme.md).
