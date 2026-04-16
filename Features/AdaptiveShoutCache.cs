using System.Collections.Generic;
using System.Linq;
using Nickel;

namespace CobaltCoreArchipelago.Features;

internal static class AdaptiveShoutCache
{
    private static Dictionary<string, List<string[]>> _mappingCache = [];
    private static Rand rng = new();

    internal static string GetLocalizedRandomLine(string[] key, bool? catBackup = null, params object[] args)
    {
        var baseLine = ModEntry.Instance.Localizations.Localize(GetRandomLine(key, catBackup));
        return string.Format(baseLine, args);
    }

    internal static string[] GetRandomLine(string[] key, bool? catBackup = null) =>
        GetMostSpecificLines(key, catBackup).Random(rng);

    internal static List<string[]> GetMostSpecificLines(string[] key, bool? catBackup = null)
    {
        var mappingKey = key.Aggregate((s1, s2) => $"{s1}/{s2}");
        if (catBackup is not null)
            mappingKey += catBackup.Value ? "/backup" : "/cat";
        
        // If the lines exist in the cache we just return them
        if (_mappingCache.TryGetValue(mappingKey, out var lines))
        {
            return lines;
        }

        // Otherwise, find a matching key in the localizations by progressively removing tokens at the end
        var loc = ModEntry.Instance.DefaultEnglishLocalizations;
        var subkey = key.ToList();
        if (subkey.Count == 0)
        {
            // If the key match completely fails, we return an empty list
            _mappingCache[mappingKey] = [];
            return [];
        }
        test_subkey:
        var subkeyFull = (
            catBackup is null ? subkey
            : catBackup.Value ? subkey.Append("backup")
            : subkey.Append("cat")
        ).ToList();
        var firstLine = loc.Localize(subkeyFull.Append("1").ToArray());
        if (firstLine is null)
        {
            subkey.RemoveAt(subkey.Count - 1);
            goto test_subkey;
        }
        
        // If we found a corresponding key, get all keys within that list
        List<string[]> res = [];
        var i = 1;
        start_enum_key:
        var enumKey = subkeyFull.Append(i.ToString()).ToArray();
        if (loc.Localize(enumKey) is null) goto finish_enum_key;
        res.Add(enumKey);
        i++;
        goto start_enum_key;
        finish_enum_key:
        _mappingCache[mappingKey] = res;
        return res;
    }
}