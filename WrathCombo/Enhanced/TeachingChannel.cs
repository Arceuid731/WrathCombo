namespace WrathCombo.Enhanced;

internal enum TeachingChannel { Damage, DamageArea, Healing, HealingArea }

internal static class TeachingChannels
{
    internal static readonly TeachingChannel[] All =
        [TeachingChannel.Damage, TeachingChannel.DamageArea, TeachingChannel.Healing, TeachingChannel.HealingArea];

    internal static bool IsHealing(this TeachingChannel channel) => channel is TeachingChannel.Healing or TeachingChannel.HealingArea;
    internal static bool IsArea(this TeachingChannel channel) => channel is TeachingChannel.DamageArea or TeachingChannel.HealingArea;
    internal static string Label(this TeachingChannel channel) => channel switch
    {
        TeachingChannel.Damage => "Damage — Single target",
        TeachingChannel.DamageArea => "Damage — AoE",
        TeachingChannel.Healing => "Healing — Single target",
        _ => "Healing — AoE",
    };
    internal static TeachingChannel From(bool healing, bool area) =>
        healing ? (area ? TeachingChannel.HealingArea : TeachingChannel.Healing) :
        (area ? TeachingChannel.DamageArea : TeachingChannel.Damage);
}
