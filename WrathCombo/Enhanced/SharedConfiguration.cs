using ECommons.DalamudServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Linq;
using WrathCombo.Core;

namespace WrathCombo.Enhanced;

internal static class SharedConfiguration
{
    private static JObject baseline = new();
    private static string? sharedContents;
    private static bool sharedLoaded;
    internal static string Status { get; private set; } = "";
    private static string SharedPath => Path.Combine(Svc.PluginInterface.ConfigFile.DirectoryName!, "WrathCombo.json");

    internal static bool OriginalLoaded => Svc.PluginInterface.InstalledPlugins.Any(
        p => p.InternalName == "WrathCombo" && p.IsLoaded);

    internal static Configuration Load()
    {
        EnhancedSettings.Load();
        var local = Svc.PluginInterface.ConfigFile.FullName;
        var useShared = EnhancedSettings.Current.ShareRotationSettings && File.Exists(SharedPath);
        var path = useShared ? SharedPath : local;
        Configuration config;
        if (File.Exists(path))
        {
            var contents = File.ReadAllText(path);
            var json = JObject.Parse(contents);
            // Ignore stale enum names for evaluation; retain them in the shared JSON.
            if (json["AutoActions"] is JObject actions)
                foreach (var key in actions.Properties().Where(p => p.Name != "$type" &&
                    !Enum.TryParse<Preset>(p.Name, out _)).ToArray()) key.Remove();
            // Never instantiate types named by configuration metadata. Declared types
            // also let us read WrathCombo's original assembly-qualified metadata.
            config = json.ToObject<Configuration>(JsonSerializer.Create(new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.None,
                ObjectCreationHandling = ObjectCreationHandling.Replace,
            })) ?? throw new InvalidDataException("Empty rotation configuration.");
            if (useShared)
            {
                sharedContents = contents;
                sharedLoaded = true;
                Status = "Shared with Wrath Combo";
            }
        }
        else config = new();
        baseline = Snapshot(config);
        return config;
    }

    private static JObject Snapshot(Configuration config)
    {
        var result = JObject.FromObject(config, JsonSerializer.Create(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
        }));
        ConfigurationMerge.UseOriginalAssembly(result);
        return result;
    }

    internal static void Save(Configuration config)
    {
        Svc.PluginInterface.SavePluginConfig(config);
        if (!EnhancedSettings.Current.ShareRotationSettings) return;
        if (OriginalLoaded)
        {
            Status = "Sharing paused: disable Wrath Combo, then reload Enhanced";
            return;
        }
        // Refuse a stale overwrite, including another process changing the file.
        var current = File.Exists(SharedPath) ? File.ReadAllText(SharedPath) : null;
        if (current != sharedContents || (!sharedLoaded && current != null))
        {
            Status = "Wrath settings changed: reload Enhanced to share them";
            return;
        }
        var snapshot = Snapshot(config);
        var merged = current == null ? snapshot : ConfigurationMerge.Apply(JObject.Parse(current), baseline, snapshot);
        var output = merged.ToString(Formatting.Indented);
        if (current != output)
        {
            // Keep the first original configuration as well as the rolling backup.
            var backup = SharedPath + ".before-enhanced.bak";
            if (current != null && !File.Exists(backup)) File.Copy(SharedPath, backup);
            AtomicJson.Write(SharedPath, output);
        }
        sharedContents = output;
        sharedLoaded = true;
        baseline = snapshot;
        Status = "Shared with Wrath Combo";
    }
}
