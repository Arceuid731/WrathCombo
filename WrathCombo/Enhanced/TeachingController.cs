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
    string TargetName, uint JobId, bool Healing, bool Area, long CreatedAt)
{
    internal TeachingChannel Channel => TeachingChannels.From(Healing, Area);
}

internal sealed unsafe class TeachingController
{
    private readonly Recommendation?[] actions = new Recommendation?[4];
    private readonly string[] diagnostics = ["Not evaluated", "Not evaluated", "Not evaluated", "Not evaluated"];
    internal Recommendation? Get(TeachingChannel channel) => actions[(int)channel];
    internal string Diagnostic(TeachingChannel channel) => diagnostics[(int)channel];

    private void Clear(string reason)
    {
        Array.Clear(actions);
        Array.Fill(diagnostics, reason);
    }
    private long nextUpdate;
    private long nextErrorLog;
    private long nextDiagnosticLog;
    private string lastDiagnostic = "";

    internal static bool CanShow => EnhancedSettings.Current.Enabled && Player.Available &&
        !Player.IsDead && !Player.Mounted && GenericHelpers.IsScreenReady() &&
        !IsOccupied() && !Svc.ClientState.IsPvP && !SharedConfiguration.OriginalLoaded &&
        (!EnhancedSettings.Current.CombatOnly || PartyInCombat());

    internal void Update()
    {
        if (!CanShow)
        {
            Clear("Guidance paused or unavailable");
            return;
        }
        var now = Environment.TickCount64;
        if (now < nextUpdate) return;
        nextUpdate = now + 80;
        Array.Clear(actions);
        try
        {
            cfg ??= new(Service.Configuration.RotationConfig);
            if (ActionReplacer.ClassLocked() || DisabledJobsPVE.Contains(Player.Job) ||
                (Service.Configuration.PenaltyPause > 0 && PlayerHasActionPenalty(false)))
            {
                Clear("Job disabled, class locked, or action penalty active");
                return;
            }
            foreach (var channel in TeachingChannels.All)
                actions[(int)channel] = Select(channel);
            var diagnostic = string.Join("; ", TeachingChannels.All.Select(channel =>
                $"{channel.Label()}: {(Get(channel) == null ? Diagnostic(channel) : "suggesting")}"));
            if (diagnostic != lastDiagnostic && now >= nextDiagnosticLog)
            {
                Svc.Log.Information($"Teaching mode: {diagnostic}");
                lastDiagnostic = diagnostic;
                nextDiagnosticLog = now + 10000;
            }
        }
        catch (Exception ex)
        {
            Clear("Rotation evaluation failed; see Dalamud log");
            if (now >= nextErrorLog)
            {
                Svc.Log.Error(ex, "Teaching mode could not evaluate the current rotation.");
                nextErrorLog = now + 10000;
            }
        }
    }

    internal Recommendation? Select(TeachingChannel channel)
    {
        if (!CanShow) return null;
        void Note(string reason) => diagnostics[(int)channel] = reason;
        var healing = channel.IsHealing();
        var area = channel.IsArea();
        var settings = EnhancedSettings.Current;
        var autoRotationEnabled = cfg!.Enabled;
        var useRotationTargeting = settings.UseWrathTargeting && autoRotationEnabled;
        if (healing && Player.Object?.Role != CombatRole.Healer &&
            !(Player.Job == Job.BLU && BLU.HasHealerMimicry))
        {
            Note("Not a healing job");
            return null;
        }

        var previousHealTarget = AutorotHealTarget;
        IBattleChara? target;
        try
        {
            target = healing
                ? (useRotationTargeting
                    ? AutoRotationHelper.GetSingleTarget(cfg!.HealerRotationMode)
                    : SimpleTarget.Stack.AllyToHeal as IBattleChara)
                : (useRotationTargeting
                    ? AutoRotationHelper.GetSingleTarget(cfg!.DPSRotationMode)
                    : SimpleTarget.HardTarget);
        }
        finally { AutorotHealTarget = previousHealTarget; }

        if (!healing)
        {
            target = RecommendationRules.DamageTarget(settings.UseWrathTargeting, autoRotationEnabled, SimpleTarget.HardTarget, target,
                candidate => candidate.IsHostile() && !candidate.IsDead && candidate.IsTargetable);
            if (target == null)
            {
                Note($"No valid enemy target (rotation targeting active: {useRotationTargeting}, mode: {cfg!.DPSRotationMode})");
                return null;
            }
        }

        var needsSingleHeal = healing && HealerTargeting.NeedsSingleTargetHeal(target);
        var needsAreaHeal = healing && HealerTargeting.CanAoEHeal();
        var cleanseTarget = healing ? (HealRetargeting.RetargetSettingOn
            ? SimpleTarget.Stack.AllyToEsuna as IBattleChara
            : target) : null;
        var needsCleanse = healing && cleanseTarget?.HasCleansableDebuff == true;
        if (!RecommendationRules.AllowChannel(channel, needsSingleHeal || needsCleanse, needsAreaHeal))
        {
            Note("No healing or cleansing needed at the configured thresholds");
            return null;
        }

        var candidates = Service.ActionReplacer.CustomCombos
            .Select(combo => (Combo: combo, Data: combo.Preset.Attributes()))
            .Where(x => x.Data is { AutoAction: not null, ReplaceSkill: not null } &&
                x.Data.AutoAction.IsHeal == healing && x.Data.AutoAction.IsAoE == area && !x.Data.IsPvP &&
                x.Data.JobInfo.Job == Player.Job.GetUpgradedJob() && IsEnabled(x.Combo.Preset));

        Note("No enabled full rotation for this job and channel");
        foreach (var entry in candidates)
        {
            var source = entry.Data.ReplaceSkill!.ActionIDs.FirstOrDefault();
            if (source == 0) continue;
            var evaluationTarget = healing && area ? Player.Object : target;
            if (healing && !area && !needsSingleHeal && needsCleanse) evaluationTarget = cleanseTarget;
            if (!healing && area && useRotationTargeting &&
                (cfg!.DPSRotationMode != API.Enum.DPSRotationMode.Manual || cfg.DPSSettings.AoEIgnoreManual))
            {
                var best = DPSTargeting.BaseSelection.MaxBy(x => NumberOfEnemiesInRange(OriginalHook(source), x, true));
                if (best != null && NumberOfEnemiesInRange(OriginalHook(source), best, true) >
                    NumberOfEnemiesInRange(OriginalHook(source), target, true)) evaluationTarget = best;
            }
            using var context = new RecommendationContext(evaluationTarget, healing);
            var result = OriginalHook(AutoRotationHelper.InvokeCombo(entry.Combo.Preset, entry.Data, ref source, evaluationTarget));
            var detail = $"{entry.Combo.Preset}: source {source}, action {result}";
            Note($"{detail}; no learned spell recommendation");
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
            Note($"{detail}; no valid recipient");
            if (resolved == null || !resolved.IsTargetable) continue;
            // Validate the suggested recipient, including a resolver that picked someone else.
            Note($"{detail}; recipient rejected by action target check");
            if (!row.TargetArea && !ActionManager.CanUseActionOnTarget(result, resolved.Struct())) continue;
            var rangeStatus = ActionManager.GetActionInRangeOrLoS(result, Player.GameObject, resolved.Struct());
            Note($"{detail}; range/line-of-sight status {rangeStatus}");
            if (rangeStatus != 0) continue;
            var actionStatus = ActionManager.Instance()->GetActionStatus(ActionType.Action, result, resolved.GameObjectId,
                checkCastingActive: false, checkRecastActive: false);
            Note($"{detail}; action status {actionStatus}");
            if (actionStatus != 0) continue;
            Note($"{detail}; casting blocked while moving");
            if (Service.Configuration.BlockSpellOnMove && TimeMoving.Ticks > 0 &&
                ActionManager.GetAdjustedCastTime(ActionType.Action, result) > 0) continue;
            // GCDs stay visible while they recharge; unavailable abilities do not.
            Note($"{detail}; ability on cooldown");
            if (row.ActionCategory.RowId == 4 && !ActionReady(result)) continue;

            Note($"{detail}; suggesting on {(resolved.GameObjectId == Player.Object?.GameObjectId ? "self" : "target")}");
            return new(result, source, resolved.GameObjectId, resolved.Name.TextValue,
                (uint)Player.Job, healing, area, Environment.TickCount64);
        }
        return null;
    }

    internal static bool IsFresh(Recommendation? action) => action != null && CanShow &&
        RecommendationRules.Fresh(action.JobId, (uint)Player.Job, action.CreatedAt, Environment.TickCount64);

    internal void Click(Recommendation displayed, bool targetOnly)
    {
        if (!CanShow) return;
        var channel = EnhancedSettings.Current.Get(displayed.Channel);
        if (channel.PassThrough || (!targetOnly && !channel.ClickToUse)) return;
        // Re-evaluate on the framework thread: never cast on a stale displayed target.
        Svc.Framework.RunOnFrameworkThread(() =>
        {
            if (!CanShow || channel.PassThrough || (!targetOnly && !channel.ClickToUse) ||
                !IsFresh(displayed)) return;
            var action = Select(displayed.Channel);
            if (action == null || action.ActionId != displayed.ActionId || action.SourceAction != displayed.SourceAction ||
                action.TargetId != displayed.TargetId) return;
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
