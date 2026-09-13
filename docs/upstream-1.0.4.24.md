# Upstream 1.0.4.24 integration

Enhanced release: **1.0.0.6**. Upstream base before integration: `736597dee5d5ee90cd043becf27e8d973fda43e0` (1.0.4.23 plus fixes). Merged upstream: [`28ae16cc316b062895bdee7d05d9d3620a93c900`](https://github.com/PunishXIV/WrathCombo/commit/28ae16cc316b062895bdee7d05d9d3620a93c900), September 13, 2026. The upstream delta spans 151 files.

## Player changes

- Beastmaster: simple rotation, basic/instinctual/intentional combos, capture helper, battlehorn lockout and Borrow options, including optional beast cycling. Its simple single-target preset carries the full-rotation metadata consumed by Enhanced guidance.
- Openers: broad job updates with countdown/prepull blocking settings, revised delay/reset/skip handling, Dancer opener changes, Reaper opener corrections and a Viper FRU opener. Prepull blocking defaults to enabled on the new settings: a recommendation can wait for a countdown rather than display an action immediately.
- Melee positionals: upcoming rear/flank information for SAM, DRG, MNK, NIN, RPR and VPR, exposed through IPC for compatible consumers. This does not add a positional overlay to Enhanced itself.
- Blue Mage: configurable Primal Combo and opener options, including manual J Kick.
- Dark Knight: mana reservation for The Blackest Night is ignored before the action is available.
- Sage: Druochole used to prevent Addersgall overcap resolves its healing target in the job logic, including when suggested by a damage rotation.
- Occult Crescent: configurable buff/debuff refresh timing, including Occult Libra, plus action adjustments.
- PvE selection separates regular jobs from limited jobs and miscellaneous features. Numerous rotations also adopt updated status helpers; the large PvP diff is mostly this helper migration.

## Integration decisions

The only textual merge conflict was the project version. Enhanced keeps its own version sequence, assembly/manifest identity, commands and release pipeline. Upstream BST resources and build settings are retained. WrathCombo.API advances from `db81ee8` to `27879ef5865e62781170f8e1a6ab1c549c04e020`; the ECommons and PunishLib pins are unchanged.

The four teaching channels, compact cards, hotbar glow, click validation, scoped retargeting and shared configuration code remain intact. Review covered all seven automatically merged shared code files, including the rotation controller, target access, action hook, preset UI, debug UI and plugin lifecycle.

A behavioral integration issue was found in the new positional service: invoking a rotation for teaching could publish its temporary target or clear a live hint. The service now ignores reports, resets and ticks within a recommendation context, and snapshot reads keep the live target. Six detached checks exercise the production service/context with boundary doubles. Running them against the original upstream service reproduces the temporary-target failure.

## Validation

- 59 detached checks, plus 3 read-only checks against the existing local Wrath configuration: **62 passed**.
- Release compilation against installed Dalamud API 15: **0 errors**. Compiler warnings remain (including obsolete upstream helpers and unused members).
- Enhanced package validation checks identity, version, dependencies, licenses and all six satellite localizations.
- No live game combat, countdown, native UI or external positional-consumer acceptance has been performed. Follow the [in-game checklist](enhanced-validation.md) for those checks.
