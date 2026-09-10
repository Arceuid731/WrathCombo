using ECommons.DalamudServices;
using Newtonsoft.Json;
using System.IO;

namespace WrathCombo.Enhanced;

internal sealed class EnhancedSettings : TeachingPreferences
{
    public static EnhancedSettings Current { get; private set; } = new();

    private static string FilePath => Path.Combine(Svc.PluginInterface.GetPluginConfigDirectory(), "TeachingMode.json");

    public static void Load()
    {
        if (File.Exists(FilePath))
            Current = JsonConvert.DeserializeObject<EnhancedSettings>(File.ReadAllText(FilePath))
                ?? throw new InvalidDataException("Empty teaching mode configuration.");
        Current.Normalize();
        Current.Save();
    }

    public void Save()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
        AtomicJson.Write(FilePath, JsonConvert.SerializeObject(this, Formatting.Indented));
    }
}
