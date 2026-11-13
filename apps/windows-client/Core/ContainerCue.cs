using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
namespace ChestTracker.Core;

/// <summary>Optional own-companion HUD feedback. A generic XP banner or visible world loot cannot satisfy this cue.</summary>
public static partial class ContainerCue
{
    public static (bool Opened, int Amount) Read(string text)
    {
        var value = new string(text.Normalize(NormalizationForm.FormD).Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark).ToArray()).ToUpperInvariant();
        var compact = string.Concat(value.Where(char.IsLetter));
        bool opened = Near(compact, "BEHALTERGEOFFNET") || Near(compact, "CONTAINEROPENED") || Near(compact, "CONTAINERSOPENED");
        var number = Amount().Match(value);
        int amount = number.Success && int.TryParse(number.Groups[1].Value, out var parsed) ? Math.Clamp(parsed, 0, 200) : 0;
        return (opened, amount);
    }
    private static bool Near(string value, string expected)
    {
        if (value.Contains(expected)) return true;
        // Approximate substring matching with two reusable DP rows; no per-character allocations.
        if (value.Length < expected.Length - 2) return false;
        var previous = Enumerable.Range(0, expected.Length + 1).ToArray();
        var current = new int[expected.Length + 1];
        foreach (char letter in value)
        {
            current[0] = 0;
            for (int j = 1; j <= expected.Length; j++)
                current[j] = Math.Min(Math.Min(current[j - 1] + 1, previous[j] + 1), previous[j - 1] + (letter == expected[j - 1] ? 0 : 1));
            if (current[^1] <= 2) return true;
            (previous, current) = (current, previous);
        }
        return false;
    }
    public static bool FeedbackCovered(string text)
    {
        var compact = string.Concat(text.ToUpperInvariant().Where(char.IsLetter));
        return compact.Contains("EVELAUFSTIEG") || compact.Contains("RAFTVERBESSERT") || compact.Contains("RAFTWERBESSERT") || compact.Contains("LEVELUP");
    }
    [GeneratedRegex(@"\+\s*(\d{1,3})\b")] private static partial Regex Amount();
}
