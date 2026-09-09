using System.IO;

namespace WrathCombo.Enhanced;

internal static class AtomicJson
{
    internal static void Write(string path, string content)
    {
        var temporary = path + ".enhanced.tmp";
        File.WriteAllText(temporary, content);
        if (File.Exists(path))
            File.Replace(temporary, path, path + ".enhanced.bak");
        else
            File.Move(temporary, path);
    }
}
