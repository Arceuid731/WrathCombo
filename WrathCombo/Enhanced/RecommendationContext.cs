using Dalamud.Game.ClientState.Objects.Types;
using System;
using System.Collections.Generic;

namespace WrathCombo.Enhanced;

/// <summary>Keep recommendation target resolvers separate from live button retargeting.</summary>
internal sealed class RecommendationContext : IDisposable
{
    [ThreadStatic] internal static RecommendationContext? Current;
    [ThreadStatic] internal static bool ExecutingClick;
    internal readonly IGameObject? Target;
    internal readonly bool Healing;
    internal readonly Dictionary<uint, Func<IGameObject?>> Retargets = new();
    private readonly RecommendationContext? previous;

    internal RecommendationContext(IGameObject? target, bool healing)
    {
        previous = Current;
        Target = target;
        Healing = healing;
        Current = this;
    }

    public void Dispose() => Current = previous;
}
