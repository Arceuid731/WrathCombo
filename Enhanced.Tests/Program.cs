using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Numerics;
using WrathCombo.Enhanced;

var passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new InvalidOperationException(name);
    passed++;
    Console.WriteLine($"PASS {name}");
}

var original = JObject.Parse("""
{"$type":"WrathCombo.Core.Configuration, WrathCombo","Unknown":{"value":7},
 "RotationConfig":{"Enabled":false,"Limit":70,"FutureOption":true},
 "EnabledActionsV6":[100,200],"CustomIntValuesV6":{"keep":1,"remove":2,"unknown":3}}
""");
var before = JObject.Parse("""
{"$type":"WrathCombo.Core.Configuration, WrathCombo","RotationConfig":{"Enabled":false,"Limit":70},
 "EnabledActionsV6":[100,200],"CustomIntValuesV6":{"keep":1,"remove":2}}
""");
var after = (JObject)before.DeepClone();
after["RotationConfig"]!["Limit"] = 45;
after["EnabledActionsV6"] = new JArray(200);
((JObject)after["CustomIntValuesV6"]!).Remove("remove");
var merged = ConfigurationMerge.Apply(original, before, after);
Check((int)merged["RotationConfig"]!["Limit"]! == 45, "Changed healing threshold exported");
Check((bool)merged["RotationConfig"]!["FutureOption"]!, "Unknown nested option preserved");
Check((int)merged["Unknown"]!["value"]! == 7, "Unknown upstream field preserved");
Check(merged["CustomIntValuesV6"]!["remove"] == null && (int)merged["CustomIntValuesV6"]!["unknown"]! == 3,
    "Known dictionary removal preserves unknown keys");
Check(JToken.DeepEquals(merged["EnabledActionsV6"], new JArray(200)), "Disabled presets are removed, not merged back");
Check(!(bool)merged["RotationConfig"]!["Enabled"]!, "Automatic execution flag preserved");
Check(JToken.DeepEquals(ConfigurationMerge.Apply(original, before, before), original), "No-op saves preserve original JSON");
Check((int)original["RotationConfig"]!["Limit"]! == 70, "Merge does not mutate input");
var types = JObject.Parse("""
{"$type":"WrathCombo.Core.Configuration, WrathComboEnhanced","Dictionary":{"$type":"System.Collections.Generic.Dictionary`2[[WrathCombo.Preset, WrathComboEnhanced],[System.Boolean, System.Private.CoreLib]], System.Private.CoreLib"}}
""");
ConfigurationMerge.UseOriginalAssembly(types);
Check(!types.ToString().Contains("WrathComboEnhanced"), "Root and nested assembly metadata compatible with original plugin");

var legacy = JObject.Parse("""
{"$type":"WrathCombo.Core.Configuration, WrathCombo","EnabledActionsV6":{"$type":"Unknown.Collection, Unavailable.Assembly","$values":[100,200]},
 "CustomIntValuesV6":{"$type":"Unknown.Dictionary, Unavailable.Assembly","Example":42},"Version":6}
""");
var serializer = JsonSerializer.Create(new JsonSerializerSettings
{ TypeNameHandling = TypeNameHandling.None, ObjectCreationHandling = ObjectCreationHandling.Replace });
var loaded = legacy.ToObject<ConfigShape>(serializer)!;
Check(loaded.EnabledActions.SetEquals([100, 200]), "Legacy typed collection metadata read without loading original assembly");
Check(ConfigShape.CustomInts["Example"] == 42, "Static job settings imported");
var serialized = JObject.FromObject(loaded, JsonSerializer.Create(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Objects }));
Check((int)serialized["CustomIntValuesV6"]!["Example"]! == 42, "Static job settings exported");

// Reproduce guidance on a selected dummy with Tank Target configured and no tank in the party.
var dummy = new TargetShape(true, true, false);
var tankTarget = new TargetShape(true, true, false);
var friendly = new TargetShape(false, true, false);
var dead = new TargetShape(true, true, true);
var untargetable = new TargetShape(true, false, false);
bool ValidDamageTarget(TargetShape t) => t.Hostile && t.Targetable && !t.Dead;
Check(ReferenceEquals(RecommendationRules.DamageTarget(true, false, dummy, null, ValidDamageTarget), dummy),
    "Auto-Rotation off with Tank Target configured guides the selected training dummy");
Check(ReferenceEquals(RecommendationRules.DamageTarget(true, false, dummy, tankTarget, ValidDamageTarget), dummy),
    "Auto-Rotation off ignores the tank target even when one exists");
Check(ReferenceEquals(RecommendationRules.DamageTarget(true, true, dummy, null, ValidDamageTarget), dummy),
    "Auto-Rotation with no tank target falls back to the selected enemy");
Check(ReferenceEquals(RecommendationRules.DamageTarget(true, true, dummy, tankTarget, ValidDamageTarget), tankTarget),
    "Available Wrath rotation target keeps priority over the selected enemy");
Check(ReferenceEquals(RecommendationRules.DamageTarget(false, true, dummy, tankTarget, ValidDamageTarget), dummy),
    "Guidance with rotation targeting disabled follows the selected enemy");
Check(ReferenceEquals(RecommendationRules.DamageTarget(true, true, dummy, friendly, ValidDamageTarget), dummy),
    "Friendly tank target falls back to the selected enemy");
Check(ReferenceEquals(RecommendationRules.DamageTarget(true, true, dummy, dead, ValidDamageTarget), dummy),
    "Dead rotation target falls back to the selected enemy");
Check(ReferenceEquals(RecommendationRules.DamageTarget(true, true, dummy, untargetable, ValidDamageTarget), dummy),
    "Untargetable rotation target falls back to the selected enemy");
Check(RecommendationRules.DamageTarget(true, true, friendly, null, ValidDamageTarget) == null,
    "Fallback never turns an ally into a damage target");
Check(RecommendationRules.DamageTarget(false, true, dead, tankTarget, ValidDamageTarget) == null,
    "Manual targeting never silently switches away from a dead selected target");
Check(RecommendationRules.DamageTarget<TargetShape>(true, true, null, null, ValidDamageTarget) == null,
    "Guidance remains idle when neither target exists");

Check(RecommendationRules.AllowChannel(TeachingChannel.Damage, false, false) &&
    RecommendationRules.AllowChannel(TeachingChannel.DamageArea, false, false), "Both damage alternatives remain visible independently of the AoE enemy-count threshold");
Check(!RecommendationRules.AllowChannel(TeachingChannel.Healing, false, true), "Healthy single target does not cause heal spam");
Check(RecommendationRules.AllowChannel(TeachingChannel.HealingArea, false, true), "Group healing remains independent of single-target need");
Check(!RecommendationRules.AllowChannel(TeachingChannel.HealingArea, true, false), "Area healing respects group thresholds");
Check(RecommendationRules.AllowChannel(TeachingChannel.Healing, true, false), "Single-target healing or cleansing stays independent of area healing");
Check(RecommendationRules.Fresh(24, 24, 100, 599) && !RecommendationRules.Fresh(24, 24, 100, 600), "Stale recommendations expire");
Check(!RecommendationRules.Fresh(24, 28, 100, 110), "Job changes invalidate recommendations");
Check(RecommendationRules.Matches(119, 1000000, 1000000, 1000000, 1000000), "Custom DPS button highlighted");
Check(RecommendationRules.Matches(119, 1000000, 119, 119, 119), "Native spell highlighted alongside custom button");
Check(!RecommendationRules.Matches(119, 1000000, 120, 1000002, 120), "Healing button not highlighted as damage");
Check(!RecommendationRules.Matches(119, 100, 100, 100, 100, false), "Teaching without replacements highlights only the suggested spell");
for (uint source = 1_000_000; source <= 1_000_003; source++)
    Check(Enumerable.Range(1_000_000, 4).All(button =>
        RecommendationRules.Matches(3593, source, 3593, (uint)button, 3593) == (button == source)),
        $"A shared Astral Draw recommendation highlights only its own custom channel {source}");

var preferences = JsonConvert.DeserializeObject<TeachingPreferences>("""
{"Damage":{"Color":{"X":0.8,"Y":0.2,"Z":0.1,"W":1},"Position":{"X":123,"Y":456},
"WindowSize":{"X":250,"Y":180},"Locked":true,"ClickThrough":true,"IconSize":48},
"Healing":{"ShowWindow":false},"Preview":true}
""")!;
preferences.Normalize();
Check(preferences.Damage.Position == new Vector2(123, 456) && preferences.Damage.Locked && preferences.Damage.PassThrough,
    "Compact migration preserves existing positions and interaction preferences");
Check(preferences.Damage.WindowSize == null && preferences.LayoutVersion == 1, "Tall legacy window sizes migrate to compact defaults");
Check(preferences.DamageArea!.Color == preferences.Damage.Color && preferences.DamageArea.Locked && !preferences.HealingArea!.ShowWindow,
    "New AoE channels inherit the corresponding saved color and visibility");
Check(preferences.DamageArea.Position == null && !preferences.Preview, "New windows get separate positions and preview does not persist");
preferences.DamageArea.Color = Vector4.One;
Check(preferences.Damage.Color != Vector4.One, "Editing an AoE color leaves the single-target color untouched");
preferences.Damage.WindowSize = new(150, 55);
preferences.DamageArea.WindowSize = new(180, 60);
var roundTrip = JsonConvert.DeserializeObject<TeachingPreferences>(JsonConvert.SerializeObject(preferences))!;
roundTrip.Normalize();
Check(roundTrip.Damage.WindowSize == new Vector2(150, 55) && roundTrip.DamageArea!.WindowSize == new Vector2(180, 60),
    "Each resized window survives reload without repeating the layout migration");
Check(TeachingChannels.All.All(c => TeachingChannels.From(c.IsHealing(), c.IsArea()) == c), "All four channel identities round-trip");
var compact = new ChannelSettings();
var compactSize = ActionWindowLayout.Size(compact, ActionWindowLayout.TextHeight(compact, 17, 2));
Check(compactSize.Y <= 55 && compactSize.X <= 160, "Default horizontal card stays compact with both text lines visible");
compact.ShowActionName = compact.ShowTarget = compact.ShowCooldown = false;
Check(ActionWindowLayout.Size(compact, 0) == new Vector2(56, 50), "Icon-only windows need no title or unused text space");
foreach (var pixels in new[] { 48, 64, 96 })
{
    var oldBounds = HotbarGlowFrame.Bounds(new Vector2(100), new Vector2(pixels), 0);
    var expanded = HotbarGlowFrame.Bounds(new Vector2(100), new Vector2(pixels), 0, 3);
    Check(expanded.Start == oldBounds.Start - new Vector2(4.5f) && expanded.End == oldBounds.End + new Vector2(4.5f),
        $"Pixel padding expands the native glow symmetrically at {pixels}px HUD size");
}

var testDir = Path.Combine(Path.GetTempPath(), "WrathEnhanced-tests-" + Guid.NewGuid());
Directory.CreateDirectory(testDir);
var testFile = Path.Combine(testDir, "config.json");
try
{
    AtomicJson.Write(testFile, "{\"value\":1}");
    AtomicJson.Write(testFile, "{\"value\":2}");
    Check((int)JObject.Parse(File.ReadAllText(testFile))["value"]! == 2, "Atomic save replaces destination");
    Check((int)JObject.Parse(File.ReadAllText(testFile + ".enhanced.bak"))["value"]! == 1, "Previous configuration backed up");
    Check(!File.Exists(testFile + ".enhanced.tmp"), "Completed save leaves no pending file");
}
finally
{
    foreach (var file in Directory.GetFiles(testDir)) File.Delete(file);
    Directory.Delete(testDir);
}

if (args.Length == 1)
{
    var bytesBefore = File.ReadAllBytes(args[0]);
    var actual = JObject.Parse(File.ReadAllText(args[0]));
    var shape = actual.ToObject<ConfigShape>(serializer)!;
    Check(shape.Version == 6 && shape.EnabledActions.Count > 0, "Existing Wrath configuration imports enabled presets");
    Check(ConfigShape.CustomInts.Count > 0, "Existing Wrath configuration imports job options");
    Check(bytesBefore.SequenceEqual(File.ReadAllBytes(args[0])), "Existing configuration left untouched by validation");
}
PositionalHintTests.Run(Check);
Console.WriteLine($"{passed} tests passed.");

internal sealed class ConfigShape
{
    public int Version { get; set; }
    [JsonProperty("EnabledActionsV6")] public HashSet<int> EnabledActions { get; set; } = [];
    [JsonProperty("CustomIntValuesV6")] internal static Dictionary<string, int> CustomInts { get; set; } = [];
}

internal sealed record TargetShape(bool Hostile, bool Targetable, bool Dead);
