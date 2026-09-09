namespace WrathCombo.Enhanced;

internal static class RecommendationRules
{
    internal static bool AllowLane(int mode, bool area, bool healing, bool needsSingle,
        bool needsArea, int enemies, int areaThreshold)
    {
        if ((mode == 1 && area) || (mode == 2 && !area)) return false;
        if (healing) return area ? needsArea : needsSingle;
        return !area || mode == 2 || enemies >= areaThreshold;
    }

    internal static bool Fresh(uint recommendationJob, uint currentJob, long created, long now) =>
        recommendationJob == currentJob && now >= created && now - created < 500;

    internal static bool Matches(uint action, uint source, uint displayed, uint original, uint adjusted, bool replacing = true) =>
        action != 0 && (displayed == action || original == action || adjusted == action || (replacing && original == source));
}
