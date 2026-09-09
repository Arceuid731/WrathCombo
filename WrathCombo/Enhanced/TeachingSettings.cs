using ECommons.DalamudServices;
using System.Globalization;

namespace WrathCombo.Enhanced;

internal static class TeachingSettings
{
    internal static string L(string english, string french) =>
        CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "fr" ? french : english;

    internal static void Draw()
    {
        if (!ImGui.CollapsingHeader(L("Teaching mode", "Mode pédagogique"), ImGuiTreeNodeFlags.DefaultOpen)) return;
        ImGui.PushID("EnhancedTeaching");
        var c = EnhancedSettings.Current;
        bool changed = ImGui.Checkbox(L("Show rotation guidance", "Afficher les conseils de rotation"), ref c.Enabled);
        changed |= ImGui.Checkbox(L("Manual play", "Jeu manuel"), ref c.ManualPlay);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(L("Use your hotbars or click an enabled action window to act.", "Utilisez vos barres ou une fenêtre d’action activée pour lancer vos sorts."));
        changed |= ImGui.Checkbox(L("In combat only", "En combat uniquement"), ref c.CombatOnly);
        changed |= ImGui.Checkbox(L("Hide windows when no action is suggested", "Masquer les fenêtres sans action conseillée"), ref c.HideWhenIdle);
        changed |= ImGui.Checkbox(L("Preview windows", "Aperçu des fenêtres"), ref c.Preview);
        changed |= ImGui.Checkbox(L("Use Wrath's rotation targeting", "Utiliser le ciblage de rotation Wrath"), ref c.UseWrathTargeting);
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(L("Otherwise: selected enemy and your healing target priorities.", "Sinon : ennemi sélectionné et vos priorités de ciblage des soins."));
        ImGui.TextWrapped(L("Enable a damage and healing preset for your job. Healing thresholds follow the Auto-Rotation settings.",
            "Activez les presets de dégâts et de soins de votre job. Les seuils de soin suivent les réglages d’Auto-Rotation."));
        changed |= ImGui.SliderFloat(L("Outline thickness", "Épaisseur des contours"), ref c.BorderWidth, 1, 8, "%.1f");
        changed |= ImGui.Checkbox(L("Pulse outlines", "Animer les contours"), ref c.Pulse);
        changed |= DrawChannel(c.Damage, false);
        changed |= DrawChannel(c.Healing, true);
        if (ImGui.Button(L("Reset window positions", "Réinitialiser les positions")))
        {
            c.Damage.Position = null;
            c.Healing.Position = null;
            // Reopen windows so the next Appearing condition applies the defaults.
            P.TeachingWindowReset++;
            changed = true;
        }
        changed |= ImGui.Checkbox(L("Share rotation settings with Wrath Combo", "Partager les réglages de rotation avec Wrath Combo"), ref c.ShareRotationSettings);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip(L("Applied when Enhanced is reloaded.", "Appliqué au rechargement d’Enhanced."));
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
        if (ImGui.TreeNodeEx(L(healing ? "Healing" : "Damage", healing ? "Soins" : "Dégâts"), ImGuiTreeNodeFlags.DefaultOpen))
        {
            changed |= ImGui.Checkbox(L("Highlight hotbar actions", "Surligner les sorts sur les barres"), ref c.Highlight);
            changed |= ImGui.ColorEdit4(L("Color", "Couleur"), ref c.Color, ImGuiColorEditFlags.NoInputs);
            changed |= ImGui.Checkbox(L("Show next action window", "Afficher la prochaine action"), ref c.ShowWindow);
            changed |= ImGui.Combo(L("Rotation", "Rotation"), ref c.Rotation,
                L("Automatic\0Single target\0Area of effect\0", "Automatique\0Monocible\0Zone\0"));
            changed |= ImGui.Checkbox(L("Lock position", "Verrouiller la position"), ref c.Locked);
            changed |= ImGui.Checkbox(L("Click through", "Laisser passer les clics"), ref c.ClickThrough);
            changed |= ImGui.Checkbox(L("Click icon to use action", "Cliquer sur l’icône pour lancer l’action"), ref c.ClickToUse);
            changed |= ImGui.Checkbox(L("Show action name", "Afficher le nom du sort"), ref c.ShowActionName);
            changed |= ImGui.Checkbox(L("Show target name", "Afficher le nom de la cible"), ref c.ShowTarget);
            changed |= ImGui.Checkbox(L("Show GCD bar", "Afficher la recharge globale"), ref c.ShowCooldown);
            changed |= ImGui.SliderFloat(L("Icon size", "Taille de l’icône"), ref c.IconSize, 32, 160, "%.0f");
            changed |= ImGui.SliderFloat(L("Background opacity", "Opacité du fond"), ref c.Opacity, 0, 1, "%.2f");
            ImGui.TreePop();
        }
        ImGui.PopID();
        return changed;
    }
}
