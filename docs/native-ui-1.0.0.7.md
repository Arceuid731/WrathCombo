# Enhanced 1.0.0.7: native UI priority and upstream refresh

Upstream merged: PunishXIV/WrathCombo main through
`41f53f6b5f3d54049f18418f9256eb61b2afd88f`, five commits after the previous merge.
Only the package version conflicted; Enhanced retains its own identity and version.
WrathCombo.API advances to `b40c88cea6e6b62981d9822637a74fd50ed9c44b`.
Teaching evaluation guards and positional IPC protection remain in place.

Dalamud ImGui draws above the game's native UI. This change provides rectangular
occlusion, rather than injecting a new native draw layer. Visible addons with window
components and native action/item tooltips and menus provide blockers. Hotbars are
excluded because their highlights belong above the buttons. Window bounds are read
each ImGui frame, including the main viewport offset, and no addon pointers are retained.

The guidance window draw commands and the highlight's own foreground command range
are split around those blockers. Geometry, texture handles and index offsets remain
unchanged. Uncovered regions are disjoint, so overlapping native windows do not multiply
opacity. Existing foreground commands from other plugins and subsequent rendering are
preserved. Guidance becomes mouse-transparent while the cursor is over a blocker.

`GameUiOnTop` defaults to true for existing configurations. The setting can be changed
in Teaching mode. The configuration window and unrelated plugin overlays are unaffected.
HUD elements without a window component and unknown nonstandard popup types are not
automatically treated as opaque windows. Rounded corners and translucent game panels
use their bounding rectangles; this is not per-pixel native compositing.

Validation commands:

```powershell
dotnet run --project Enhanced.Tests -c Release
dotnet run --project Enhanced.UiTests -c Release
dotnet build WrathCombo/WrathCombo.csproj -c Release --output artifacts/build
./.github/scripts/validate-package.ps1
```

The native test creates a separate ImGui context and exercises the actual draw-command
buffer manipulation. It does not load a game client or alter player configuration.
In-game tooltip bounds, HUD scales and input behavior remain to be confirmed by the player.