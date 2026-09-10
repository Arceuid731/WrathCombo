Wrath Combo Enhanced 1.0.0.5

- Four independent guidance channels: Damage and Healing, each with Single target and AoE suggestions. Enable the corresponding job presets to use them.
- AoE highlights have a second outer frame, retaining your damage and healing colors. Shared spells avoid duplicate glows, and custom buttons only highlight for their own channel.
- Highlight padding (px) expands the frame around each icon. The default adds 3 pixels on each side; choose from 0 to 20 in Settings → Teaching mode.
- Next-action windows are compact horizontal cards with no title, a subtle color accent, an optional thin GCD bar and an AoE badge. Each remains independently movable, resizable, lockable and optionally clickable.
- English settings and tooltips cover all four channels.

Update through the existing Dalamud repository. Existing colors, positions, visibility, targeting and click settings are preserved. Window sizes adopt the compact layout once; subsequent resizing is saved. New AoE windows inherit their role's settings and start below the single-target windows.

Both damage alternatives can appear against one enemy. Healing suggestions continue to follow Wrath's single-target and group healing thresholds. Explicit clicks recheck the same channel and source before using its action.

Validation: 53 detached checks plus 3 read-only checks against an existing Wrath configuration passed, and the Release build and package validation succeeded. Native glow geometry was rendered with the installed game's Standard Step icon at 48, 64 and 96 pixels, with 0, 3 and 8 pixels of padding. Compact card dimensions were checked separately; final in-game layout and combat validation remain to be confirmed.
