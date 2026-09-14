using Dalamud.Bindings.ImGui;
using ECommons.DalamudServices;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Common.Math;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace WrathCombo.Enhanced;

internal static unsafe class GameUiOcclusion
{
    private static int frame = -1;
    private static readonly List<UiRect> blockers = [];
    private static readonly string[] Popups =
        ["ActionDetail", "ItemDetail", "Tooltip", "_Tooltip", "ContextMenu", "ContextIconMenu", "ContextMenuTitle",
         "SelectString", "SelectIconString", "SelectYesno"];

    internal static IReadOnlyList<UiRect> Blockers
    {
        get
        {
            if (!EnhancedSettings.Current.GameUiOnTop) return Array.Empty<UiRect>();
            var current = ImGui.GetFrameCount();
            if (frame == current) return blockers;
            frame = current;
            blockers.Clear();
            var manager = RaptureAtkUnitManager.Instance();
            if (manager == null) return blockers;
            var units = &manager->AllLoadedUnitsList;
            for (var i = 0; i < Math.Min((int)units->Count, units->Entries.Length); i++)
            {
                var addon = units->Entries[i].Value;
                if (addon == null || addon->WindowNode == null) continue;
                // Hotbars are the highlight's own base layer.
                var name = addon->NameString;
                if (name.StartsWith("_Action", StringComparison.Ordinal)) continue;
                Add(addon);
            }
            // Native tooltips and popup menus do not always have a Window component.
            foreach (var name in Popups)
                Add((AtkUnitBase*)Svc.GameGui.GetAddonByName(name).Address);
            return blockers;
        }
    }

    private static void Add(AtkUnitBase* addon)
    {
        if (addon == null || !addon->IsVisible || addon->RootNode == null || !addon->RootNode->IsVisible()) return;
        Bounds bounds;
        addon->GetWindowBounds(&bounds);
        var viewport = ImGui.GetMainViewport().Pos;
        var rectangle = new UiRect(bounds.Pos1.X + viewport.X, bounds.Pos1.Y + viewport.Y,
            bounds.Pos2.X + viewport.X, bounds.Pos2.Y + viewport.Y);
        if (rectangle.Valid && !blockers.Contains(rectangle)) blockers.Add(rectangle);
    }

    internal static bool CoversMouse()
    {
        var mouse = ImGui.GetMousePos();
        foreach (var rectangle in Blockers) if (rectangle.Contains(mouse)) return true;
        return false;
    }

    internal static void Clip(ImDrawListPtr draw, int firstCommand = 0) => UiDrawClipper.Apply(draw, firstCommand, Blockers);
}