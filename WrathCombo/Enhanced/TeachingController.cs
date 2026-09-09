using Dalamud.Game.ClientState.Objects.Types;
using ECommons;
using ECommons.DalamudServices;
using ECommons.ExcelServices;
using ECommons.GameFunctions;
using ECommons.GameHelpers;
using FFXIVClientStructs.FFXIV.Client.Game;
using System;
using System.Linq;
using WrathCombo.AutoRotation;
using WrathCombo.Combos.PvE;
using WrathCombo.Core;
using WrathCombo.CustomComboNS;
using WrathCombo.Extensions;
using WrathCombo.Services;
using static WrathCombo.AutoRotation.AutoRotationController;
using static WrathCombo.CustomComboNS.Functions.CustomComboFunctions;
using ActionRow = Lumina.Excel.Sheets.Action;

namespace WrathCombo.Enhanced;

internal sealed record Recommendation(uint ActionId, uint SourceAction, ulong TargetId,
    string TargetName, uint JobId, bool Healing, bool Area, long CreatedAt);

internal sealed unsafe class TeachingController
{
    internal Recommendation? Damage { get; private set; }
    internal Recommendation? Healing { get; private set; }
    private long nextUpdate;
    private long nextErrorLog;

    internal static bool CanShow => EnhancedSettings.Current.Enabled && Player.Available &&
        !Player.IsDead && !Player.Mounted && GenericHelpers.IsScreenReady() &&
        !IsOccupied() && !Svc.ClientState.IsPvP && !SharedConfiguration.OriginalLoaded &&
        (!EnhancedSettings.Current.CombatOnly || PartyInCombat());

    internal void Update()
    {
        if (!CanShow)
        {
            Damage = Healing = null;
            return;
        }
        var now = Environment.TickCount64;
        if (now < nextUpdate) return;
        nextUpdate = now + 80;
        Damage = Healing = null;
        try
        {
            cfg ??= new(Service.Configuration.RotationConfig);
            if (ActionReplacer.ClassLocked() || DisabledJobsPVE.Contains(Player.Job) ||
                (Service.Configuration.PenaltyPause > 0 && PlayerHasActionPenalty(false))) return;
            Damage = Select(false);
            Healing = Select(true);
        }
        catch (Exception ex)
        {
            if (now >= nextErrorLog)
            {
                Svc.Log.Error(ex, "Teaching mode could not evaluate the current rotation.");
                nextErrorLog = now + 10000;
            }
        }
    }

    internal Recommendation? Select(bool healing)
    {
        if (!CanShow) return null;
        var settings = EnhancedSettings.Current;
        var channel = healing ? settings.Healing : settings.Damage;
        if (healing && Player.Object?.Role != CombatRole.Healer &&
            !(Player.Job == Job.BLU && BLU.HasHealerMimicry)) return null;

        var previousHealTarget = AutorotHealTarget;
        IBattleChara? target;
        try
        {
            target = healing
                ? (settings.UseWrathTargeting
                    ? AutoRotationHelper.GetSingleTarget(cfg!.HealerRotationMode)
                    : SimpleTarget.Stack.AllyToHeal as IBattleChara)
                : (settings.UseWrathTargeting
                    ? AutoRotationHelper.GetSingleTarget(cfg!.DPSRotationMode)
                    : SimpleTarget.HardTarget);
        }
        finally { AutorotHealTarget = previousHealTarget; }

        if (!healing && (target == null || !target.IsHostile() || target.IsDead || !target.IsTargetable))
            return null;

        var needsSingleHeal = healing && HealerTargeting.NeedsSingleTargetHeal(target);
        var needsAreaHeal = healing && HealerTargeting.CanAoEHeal();
        // Urgent single-target healing comes first even when several allies are hurt.
        var urgent = needsSingleHeal && GetTargetHPPercent(target) <= 35;
        var cleanseTarget = healing ? (HealRetargeting.RetargetSettingOn
            ? SimpleTarget.Stack.AllyToEsuna as IBattleChara
            : target) : null;
        var needsCleanse = healing && cleanseTarget?.HasCleansableDebuff == true;
        if (healing && !needsSingleHeal && !needsAreaHeal && !needsCleanse) return null;

        var candidates = Service.ActionReplacer.CustomCombos
            .Select(combo => (Combo: combo, Data: combo.Preset.Attributes()))
            .Where(x => x.Data is { AutoAction: not null, ReplaceSkill: not null } &&
                x.Data.AutoAction.IsHeal == healing && !x.Data.IsPvP &&
                x.Data.JobInfo.Job == Player.Job.GetUpgradedJob() && IsEnabled(x.Combo.Preset))
            .OrderBy(x => healing && urgent ? x.Data.AutoAction!.IsAoE : !x.Data.AutoAction!.IsAoE);

        foreach (var entry in candidates)
        {
            var area = entry.Data.AutoAction!.IsAoE;
            var source = entry.Data.ReplaceSkill!.ActionIDs.FirstOrDefault();
            if (source == 0) continue;
            var evaluationTarget = healing && area ? Player.Object : target;
            if (healing && !area && !needsSingleHeal && needsCleanse) evaluationTarget = cleanseTarget;
            if (!healing && area && settings.UseWrathTargeting &&
                (cfg!.DPSRotationMode != API.Enum.DPSRotationMode.Manual || cfg.DPSSettings.AoEIgnoreManual))
            {
                var best = DPSTargeting.BaseSelection.MaxBy(x => NumberOfEnemiesInRange(OriginalHook(source), x, true));
                if (best != null && NumberOfEnemiesInRange(OriginalHook(source), best, true) >
                    NumberOfEnemiesInRange(OriginalHook(source), target, true)) evaluationTarget = best;
            }
            var enemies = !healing && area ? NumberOfEnemiesInRange(OriginalHook(source), evaluationTarget, true) : 0;
            if (!RecommendationRules.AllowLane(channel.Rotation, area, healing, needsSingleHeal || needsCleanse,
                needsAreaHeal, enemies, cfg!.DPSSettings.DPSAoETargets ?? 3))
                continue;

            using var context = new RecommendationContext(evaluationTarget, healing);
            var result = OriginalHook(AutoRotationHelper.InvokeCombo(entry.Combo.Preset, entry.Data, ref source, evaluationTarget));
            if (healing && !needsSingleHeal && !needsAreaHeal && result is not (RoleActions.Healer.Esuna or BLU.Exuviation)) continue;
            if (result == 0 || result >= All.SingleTargetDPS || !ActionLearned(result) ||
                !Svc.Data.GetExcelSheet<ActionRow>().TryGetRow(result, out var row)) continue;

            IGameObject? resolved = evaluationTarget;
            if (context.Retargets.TryGetValue(result, out var resolver)) resolved = resolver();
            if (healing && result == RoleActions.Healer.Esuna &&
                (resolved as IBattleChara)?.HasCleansableDebuff != true) continue;
            if (row.CanTargetSelf && !row.CanTargetHostile && !row.CanTargetParty)
                resolved = Player.Object;
            if (row.TargetArea && resolved == null) resolved = Player.Object;
            if (resolved == null || !resolved.IsTargetable) continue;
            // Validate the suggested recipient, including a resolver that picked someone else.
            if (!row.TargetArea && !ActionManager.CanUseActionOnTarget(result, resolved.Struct())) continue;
            if (ActionManager.GetActionInRangeOrLoS(result, Player.GameObject, resolved.Struct()) != 0) continue;
            if (ActionManager.Instance()->GetActionStatus(ActionType.Action, result, resolved.GameObjectId,
                checkCastingActive: false, checkRecastActive: false) != 0) continue;
            if (Service.Configuration.BlockSpellOnMove && TimeMoving.Ticks > 0 &&
                ActionManager.GetAdjustedCastTime(ActionType.Action, result) > 0) continue;
            // GCDs stay visible while they recharge; unavailable abilities do not.
            if (row.ActionCategory.RowId == 4 && !ActionReady(result)) continue;

            return new(result, source, resolved.GameObjectId, resolved.Name.TextValue,
                (uint)Player.Job, healing, area, Environment.TickCount64);
        }
        return null;
    }

    internal static bool IsFresh(Recommendation? action) => action != null && CanShow &&
        RecommendationRules.Fresh(action.JobId, (uint)Player.Job, action.CreatedAt, Environment.TickCount64);

    internal void Click(Recommendation displayed, bool targetOnly)
    {
        var healing = displayed.Healing;
        if (!CanShow) return;
        var channel = healing ? EnhancedSettings.Current.Healing : EnhancedSettings.Current.Damage;
        if (channel.ClickThrough || (!targetOnly && !channel.ClickToUse)) return;
        // Re-evaluate on the framework thread: never cast on a stale displayed target.
        Svc.Framework.RunOnFrameworkThread(() =>
        {
            if (!CanShow || channel.ClickThrough || (!targetOnly && !channel.ClickToUse) ||
                !IsFresh(displayed)) return;
            var action = Select(healing);
            if (action == null || action.ActionId != displayed.ActionId || action.TargetId != displayed.TargetId) return;
            var target = action.TargetId.GetObject();
            if (target == null || !target.IsTargetable) return;
            if (targetOnly)
            {
                Svc.Targets.Target = target;
                return;
            }
            RecommendationContext.ExecutingClick = true;
            try
            {
                if (Svc.Data.GetExcelSheet<ActionRow>().GetRow(action.ActionId).TargetArea)
                {
                    var position = target.Position;
                    ActionManager.Instance()->UseActionLocation(ActionType.Action, action.ActionId, target.GameObjectId, &position);
                }
                else
                    ActionManager.Instance()->UseAction(ActionType.Action, action.ActionId, target.GameObjectId);
            }
            finally { RecommendationContext.ExecutingClick = false; }
        });
    }
}
