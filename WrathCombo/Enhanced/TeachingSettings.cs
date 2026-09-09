namespace WrathCombo.Enhanced;

internal static class TeachingSettings
{
    internal static void Tooltip(string text)
    {
        if (!ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled)) return;
        ImGui.BeginTooltip();
        ImGui.PushTextWrapPos(ImGui.GetFontSize() * 24);
        ImGui.TextUnformatted(text);
        ImGui.PopTextWrapPos();
        ImGui.EndTooltip();
    }

    private static bool Toggle(string label, ref bool value, string help)
    {
        var changed = ImGui.Checkbox(label, ref value);
        Tooltip(help);
        return changed;
    }

    internal static void Draw()
    {
        if (!ImGui.CollapsingHeader("Teaching mode", ImGuiTreeNodeFlags.DefaultOpen)) return;
        ImGui.PushID("EnhancedTeaching");
        var c = EnhancedSettings.Current;
        bool changed = Toggle("Show rotation guidance", ref c.Enabled,
            "Show the next actions from your enabled job presets, in the windows and on your hotbars.");
        changed |= Toggle("In combat only", ref c.CombatOnly,
            "Hide guidance outside combat. When off, damage suggestions still need a valid enemy target.");
        changed |= Toggle("Hide windows when no action is suggested", ref c.HideWhenIdle,
            "Hide each window when it has no suggestion. Turn this off to keep an empty window visible.");
        changed |= Toggle("Preview windows", ref c.Preview,
            "Show sample actions so you can arrange the windows. Sample actions cannot be used.");
        changed |= Toggle("Use Wrath's rotation targeting", ref c.UseWrathTargeting,
            "Follow Auto-Rotation's target modes while it is enabled. Otherwise, use your selected enemy and normal healing target priorities. If Auto-Rotation finds no valid damage target, use your selected enemy.");
        changed |= ImGui.Combo("Highlight style", ref c.HighlightStyle, "Glow\0Outline\0");
        Tooltip("Glow adds a soft, luminous frame. Outline draws a plain border. Both use your Damage and Healing colors.");
        if (c.HighlightStyle == 0)
        {
            changed |= ImGui.SliderFloat("Glow intensity", ref c.GlowIntensity, 0.2f, 2, "%.1f");
            Tooltip("Make the glow softer or stronger without changing its color.");
        }
        else
        {
            changed |= ImGui.SliderFloat("Outline thickness", ref c.BorderWidth, 1, 8, "%.1f");
            Tooltip("Set the width of the colored outline around suggested hotbar actions.");
        }
        changed |= Toggle("Pulse highlights", ref c.Pulse,
            "Gently brighten and dim the highlights to make them easier to spot.");
        changed |= DrawChannel(c.Damage, false);
        changed |= DrawChannel(c.Healing, true);
        if (ImGui.Button("Reset window layout"))
        {
            c.Damage.Position = c.Healing.Position = null;
            c.Damage.WindowSize = c.Healing.WindowSize = null;
            P.TeachingWindowReset++;
            changed = true;
        }
        Tooltip("Restore both windows to their starting positions and compact sizes.");
        changed |= Toggle("Share rotation settings with Wrath Combo", ref c.ShareRotationSettings,
            "Keep job and rotation settings shared when switching between the two plugins. Window preferences stay separate. Reload Enhanced after changing this option.");
        if (SharedConfiguration.Status.Length > 0 && c.ShareRotationSettings)
            ImGui.TextDisabled(SharedConfiguration.Status);
        if (changed) c.Save();
        ImGui.PopID();
        ImGui.Separator();
    }

    private static bool DrawChannel(ChannelSettings c, bool healing)
    {
        ImGui.PushID(healing ? "Healing" : "Damage");
        var changed = false;
        if (ImGui.TreeNodeEx(healing ? "Healing" : "Damage", ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= Toggle("Highlight hotbar actions", ref c.Highlight,
                healing
                    ? "Highlight the next action from the healing rotation, including matching custom buttons. Shared support actions can also appear in Damage."
                    : "Highlight the next action from the damage rotation, including matching custom buttons. Shared support actions can also appear in Healing.");
            changed |= ImGui.ColorEdit4("Color", ref c.Color, ImGuiColorEditFlags.NoInputs);
            Tooltip("Choose the color and transparency of this channel's highlights and window accents.");
            changed |= Toggle("Show next action window", ref c.ShowWindow,
                "Show this channel's next action in its own window. Hotbar highlights can stay on independently.");
            changed |= ImGui.Combo("Rotation", ref c.Rotation, "Automatic\0Single target\0Area of effect\0");
            Tooltip(healing
                ? "Automatic chooses between your enabled single-target and group healing presets using Wrath's healing thresholds. The other choices restrict suggestions to one type."
                : "Automatic chooses between your enabled single-target and area presets using Wrath's enemy-count threshold. The other choices restrict suggestions to one type.");
            changed |= Toggle("Lock window", ref c.Locked,
                "Keep this window in place and at its current size. Unlock it to drag the window or resize it from a corner.");
            changed |= Toggle("Click through when locked", ref c.ClickThrough,
                "While locked, let clicks pass through to the game. Unlocked windows remain interactive so you can move and resize them.");
            changed |= Toggle("Click icon to use action", ref c.ClickToUse,
                "Click the icon to use the displayed action on its suggested target. Each click requests one action. Click-through takes priority while locked.");
            changed |= Toggle("Show action name", ref c.ShowActionName,
                "Show the action name below its icon. Hover over a shortened name to read it in full.");
            changed |= Toggle("Show target name", ref c.ShowTarget,
                "Show the suggested target below the action. Green means yourself or your selected target; orange means another target. Click the name to select it.");
            changed |= Toggle("Show GCD bar", ref c.ShowCooldown,
                "Show the shared cooldown of your spells and weaponskills. A full bar means the next one can start.");
            changed |= ImGui.SliderFloat("Icon size", ref c.IconSize, 24, 160, "%.0f");
            Tooltip("Set the preferred icon size. It also shrinks to fit when you make the window smaller.");
            changed |= ImGui.SliderFloat("Background opacity", ref c.Opacity, 0, 1, "%.2f");
            Tooltip("Make the window background more transparent or more solid.");
            ImGui.TreePop();
        }
        ImGui.PopID();
        return changed;
    }
}
