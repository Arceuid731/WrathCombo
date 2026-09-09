using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

Check(!RecommendationRules.AllowLane(0, true, false, false, false, 2, 3), "DPS area threshold respected");
Check(RecommendationRules.AllowLane(0, true, false, false, false, 3, 3), "DPS area selected at threshold");
Check(RecommendationRules.AllowLane(2, true, false, false, false, 1, 3), "Forced area mode respected");
Check(!RecommendationRules.AllowLane(1, true, false, false, false, 5, 3), "Forced single-target excludes area");
Check(!RecommendationRules.AllowLane(0, false, true, false, true, 0, 3), "Healthy single target does not cause heal spam");
Check(RecommendationRules.AllowLane(0, true, true, false, true, 0, 3), "Group healing remains independent of single-target need");
Check(RecommendationRules.Fresh(24, 24, 100, 599) && !RecommendationRules.Fresh(24, 24, 100, 600), "Stale recommendations expire");
Check(!RecommendationRules.Fresh(24, 28, 100, 110), "Job changes invalidate recommendations");
Check(RecommendationRules.Matches(119, 1000000, 1000000, 1000000, 1000000), "Custom DPS button highlighted");
Check(RecommendationRules.Matches(119, 1000000, 119, 119, 119), "Native spell highlighted alongside custom button");
Check(!RecommendationRules.Matches(119, 1000000, 120, 1000002, 120), "Healing button not highlighted as damage");
Check(!RecommendationRules.Matches(119, 100, 100, 100, 100, false), "Teaching without replacements highlights only the suggested spell");

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
Console.WriteLine($"{passed} tests passed.");

internal sealed class ConfigShape
{
    public int Version { get; set; }
    [JsonProperty("EnabledActionsV6")] public HashSet<int> EnabledActions { get; set; } = [];
    [JsonProperty("CustomIntValuesV6")] internal static Dictionary<string, int> CustomInts { get; set; } = [];
}
