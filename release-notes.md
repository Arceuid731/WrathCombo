Wrath Combo Enhanced 1.0.0.2

- With Auto-Rotation off, guidance now uses your selected enemy and normal healing priorities. Inactive Auto-Rotation modes such as Tank Target no longer suppress or redirect manual guidance.
- While Auto-Rotation is enabled, Use Wrath's rotation targeting follows its target modes and falls back to your selected enemy if no valid damage target is found.
- Remove the extra Manual play switch. The native Auto-Rotation settings, DTR toggle and IPC integrations control automatic execution. Existing native settings are preserved.
- Add Teaching mode diagnostics to the Debug tab, with throttled idle-reason logging for missing recommendations.
- Fix the job settings panel throwing an exception while a custom-action icon is still loading.

Update through the existing Dalamud repository. To test the reported case, keep native Auto-Rotation off, select a training dummy as Dancer with Simple Single Target enabled, and check the next-damage window and highlights.

Targeting with Auto-Rotation off/on and missing targets has regression coverage. Live action suggestions and hotbar alignment still require in-game confirmation.
