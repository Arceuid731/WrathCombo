using Newtonsoft.Json.Linq;
using System.Linq;

namespace WrathCombo.Enhanced;

/// <summary>Apply only this session's changes, retaining unknown upstream fields.</summary>
internal static class ConfigurationMerge
{
    internal static JObject Apply(JObject original, JObject before, JObject after)
    {
        var result = (JObject)original.DeepClone();
        foreach (var property in after.Properties())
        {
            if (property.Name == "$type" || JToken.DeepEquals(before[property.Name], property.Value))
                continue;
            if (property.Value is JObject newObject && before[property.Name] is JObject oldObject
                && result[property.Name] is JObject destination)
                result[property.Name] = Apply(destination, oldObject, newObject);
            else
                result[property.Name] = property.Value.DeepClone();
        }
        foreach (var property in before.Properties().Where(p => p.Name != "$type" && after[p.Name] == null))
            result.Remove(property.Name);
        return result;
    }

    internal static void UseOriginalAssembly(JToken token)
    {
        if (token is JObject obj)
        {
            if (obj["$type"] is JValue type && type.Value is string name)
                type.Value = name.Replace(", WrathComboEnhanced", ", WrathCombo");
            foreach (var property in obj.Properties())
                UseOriginalAssembly(property.Value);
        }
        else if (token is JArray array)
            foreach (var child in array) UseOriginalAssembly(child);
    }
}
