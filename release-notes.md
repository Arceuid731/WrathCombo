Wrath Combo Enhanced 1.0.0.3

- Hotbar highlights now default to a soft glow using the game's luminous action frame, with the existing Damage and Healing colors.
- Adjust Glow intensity and Pulse highlights in Settings → Teaching mode. Choose Highlight style → Outline to keep the previous border.
- The glow scales with the hotbar icon and keeps its center transparent. Frames are nested when both channels suggest the same slot.
- Channel tooltips explain that support actions can be suggested by either the damage or healing rotation.
- The frame texture is loaded once, disposed on unload, and falls back to outlines if unavailable.

Update through the existing Dalamud repository. Your saved colors, window layouts and targeting preferences are preserved.

The texture and frame geometry were rendered against the installed game's Standard Step icon at 48, 64 and 96 pixels. In-game alignment still needs confirmation. Rotation selection and Astrologian's shared Astral Draw behavior are unchanged.
