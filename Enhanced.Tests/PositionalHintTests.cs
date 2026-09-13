using Dalamud.Game.ClientState.Objects.Types;
using ECommons.DalamudServices;
using WrathCombo.API;
using WrathCombo.API.Enum;
using WrathCombo.CustomComboNS.Functions;
using WrathCombo.Enhanced;
using WrathCombo.Services.IPC;

internal static class PositionalHintTests
{
    internal static void Run(Action<bool, string> check)
    {
        var live = new HintTarget(100);
        var preview = new HintTarget(200);
        CustomComboFunctions.LiveTarget = live;
        UpcomingPositionalHintService.Report(PositionalDirection.Rear, 7481, 1);
        var notifications = Svc.PluginInterface.Provider.Messages;
        check(PositionalHintSnapshot.TryFromWire(UpcomingPositionalHintService.GetWireSnapshot(), out var initial) &&
              initial.TargetObjectId == 100 && initial.ActionId == 7481 && notifications == 1,
            "Live positional report publishes its target and notifies subscribers");

        using (var context = new RecommendationContext(preview, false))
        {
            UpcomingPositionalHintService.Report(PositionalDirection.Flank, 7482, 2);
            UpcomingPositionalHintService.Tick();
            check(PositionalHintSnapshot.TryFromWire(UpcomingPositionalHintService.GetWireSnapshot(), out var during) &&
                  during.TargetObjectId == 100 && during.ActionId == 7481 &&
                  Svc.PluginInterface.Provider.Messages == notifications && CustomComboFunctions.ReporterTicks == 0,
                "Teaching evaluation cannot publish or refresh a temporary positional target");
            using (new RecommendationContext(null, true))
                UpcomingPositionalHintService.Reset();
            check(ReferenceEquals(RecommendationContext.Current, context) &&
                  UpcomingPositionalHintService.GetWireSnapshot() is not null &&
                  Svc.PluginInterface.Provider.Messages == notifications,
                "Nested healing evaluation cannot clear the live positional hint");
        }

        check(RecommendationContext.Current is null, "Teaching restores the live context after evaluation");
        UpcomingPositionalHintService.Report(PositionalDirection.Flank, 7482, 2);
        check(PositionalHintSnapshot.TryFromWire(UpcomingPositionalHintService.GetWireSnapshot(), out var next) &&
              next.ActionId == 7482 && next.TargetObjectId == 100 &&
              Svc.PluginInterface.Provider.Messages == notifications + 1,
            "Live positional updates resume after teaching evaluation");
        UpcomingPositionalHintService.Reset();
        check(UpcomingPositionalHintService.GetWireSnapshot() is null &&
              Svc.PluginInterface.Provider.Messages == notifications + 2,
            "Live reset still clears the positional hint and notifies subscribers");
    }
}

// Detached boundary doubles: the production context, hint service and wire model run unchanged.
internal sealed record HintTarget(ulong GameObjectId) : IBattleChara
{
    public bool IsDead => false;
}

namespace Dalamud.Game.ClientState.Objects.Types
{
    public interface IGameObject { ulong GameObjectId { get; } }
    public interface IBattleChara : IGameObject { bool IsDead { get; } }
}

namespace Dalamud.Plugin.Ipc
{
    public interface ICallGateProvider<T> { void SendMessage(); }
}

namespace ECommons.DalamudServices
{
    internal static class Svc
    {
        internal static FakePluginInterface PluginInterface { get; } = new();
    }
    internal sealed class FakePluginInterface
    {
        internal FakeProvider Provider { get; } = new();
        internal Dalamud.Plugin.Ipc.ICallGateProvider<object> GetIpcProvider<T>(string name) => Provider;
    }
    internal sealed class FakeProvider : Dalamud.Plugin.Ipc.ICallGateProvider<object>
    {
        internal int Messages;
        public void SendMessage() => Messages++;
    }
}

namespace ECommons.Throttlers
{
    internal static class EzThrottler
    {
        internal static bool Throttle(string name, int milliseconds) => true;
    }
}

namespace WrathCombo.CustomComboNS.Functions
{
    internal static class CustomComboFunctions
    {
        internal enum AttackAngle { Unknown, Front, Flank, Rear }
        internal static IBattleChara? LiveTarget;
        internal static int ReporterTicks;
        internal static IBattleChara? CurrentTarget => RecommendationContext.Current is { } context
            ? context.Target as IBattleChara : LiveTarget;
        internal static bool HasBattleTarget() => CurrentTarget is not null;
        internal static bool TargetNeedsPositionals(IBattleChara? target = null) => (target ?? CurrentTarget) is not null;
        internal static AttackAngle AngleToTarget(IBattleChara target) => AttackAngle.Rear;
        internal static float GCDTotal => 2.5f;
        internal static void TickPositionalHintReporters() => ReporterTicks++;
    }
}

namespace WrathCombo.Extensions
{
    internal static class HintObjectExtensions
    {
        internal static IGameObject? GetObject(this ulong id) =>
            CustomComboFunctions.LiveTarget?.GameObjectId == id ? CustomComboFunctions.LiveTarget : null;
    }
}
