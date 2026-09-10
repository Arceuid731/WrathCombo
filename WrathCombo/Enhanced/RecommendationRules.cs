using System;

namespace WrathCombo.Enhanced;

internal static class RecommendationRules
{
    internal static T? DamageTarget<T>(bool useRotationTargeting, bool autoRotationEnabled, T? selected, T? rotationTarget,
        Func<T, bool> valid) where T : class
    {
        if (useRotationTargeting && autoRotationEnabled && rotationTarget != null && valid(rotationTarget)) return rotationTarget;
        return selected != null && valid(selected) ? selected : null;
    }

    // Show both damage alternatives independently, as the custom buttons do.
    // Healing still observes Wrath's single-target and group healing thresholds.
    internal static bool AllowChannel(TeachingChannel channel, bool needsSingle, bool needsArea) =>
        !channel.IsHealing() || (channel.IsArea() ? needsArea : needsSingle);

    internal static bool Fresh(uint recommendationJob, uint currentJob, long created, long now) =>
        recommendationJob == currentJob && now >= created && now - created < 500;

    internal static bool Matches(uint action, uint source, uint displayed, uint original, uint adjusted, bool replacing = true) =>
        action != 0 && (original is >= 1_000_000 and <= 1_000_003
            ? replacing && original == source
            : displayed == action || original == action || adjusted == action || (replacing && original == source));
}
