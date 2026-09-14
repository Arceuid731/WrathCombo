Wrath Combo Enhanced 1.0.0.7

- Game tooltips, popup menus and framed game windows now take priority over the four guidance panels and hotbar highlights. Only the covered portions are hidden.
- Added "Keep game windows on top", enabled by default. Covered guidance panels also stop intercepting the mouse over native game windows.
- Merged all five upstream commits through 41f53f6b5, including Wrath Combo 1.0.4.25, the opener skip timeout, Beastmaster IPC updates and the queued Cease/custom-action fix.
- Preserved Enhanced guidance, glow and AoE highlights, saved positions, click controls and shared rotation settings.

Validation: Enhanced checks, Release compilation, package identity validation and isolated native ImGui clipping tests. The clipping tests cover overlapping windows, complete and partial occlusion, index/texture preservation and foreground drawings from other plugins. Native addon bounds and mouse interaction still need confirmation in the game client.