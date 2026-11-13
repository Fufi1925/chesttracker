using System.Text;
using System.Text.RegularExpressions;
namespace ChestTracker.Core;
public static partial class HudTextDetector
{
    private static string Normalize(string value) => string.Join(' ', value.Normalize(NormalizationForm.FormKC).ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    public static Observation Analyze(DateTimeOffset at, string gameplayHud, string lobbyHud, string notificationHud, string loadingHud, bool gameplayVisual = false, bool mapVisual = false)
    {
        var game=Normalize(gameplayHud);var lobby=Normalize(lobbyHud+" "+loadingHud);var message=Normalize(notificationHud);var loading=Normalize(loadingHud);
        bool lobbyCue=PlayWord().IsMatch(lobby)&&(ReadyWord().IsMatch(lobby)||BattleRoyale().IsMatch(lobby));
        bool matchCue=gameplayVisual || ((mapVisual||game.Contains("STURM")||game.Contains("STORM"))&&Clock().IsMatch(game));
        bool ended=loading.Contains("DEINE PLATZIERUNG")||loading.Contains("YOU PLACED")||loading.Contains("ELIMINIERT VON")||loading.Contains("ELIMINATED BY")||loading.Contains("EPISCHER SIEG")||loading.Contains("VICTORY ROYALE");
        var state=ended?GameState.MATCH_END_PENDING:lobbyCue&&!matchCue?GameState.LOBBY:matchCue&&!lobbyCue?GameState.IN_MATCH:
            loading.Contains("VERBINDE")||loading.Contains("CONNECTING")||loading.Contains("LADEVORGANG")||loading.Contains("VERBINDUNG HER")?GameState.LOADING:GameState.UNKNOWN;
        bool opening=message.Contains("TRUHE DURCHSUCHT")||message.Contains("TRUHEN DURCHSUCHT")||message.Contains("CHEST SEARCHED")||message.Contains("SEARCHED CHEST")||(ChestWord().IsMatch(message)&&OwnXp().IsMatch(message));
        bool teammate=message.Contains("TEAMMATE")||message.Contains("TEAMKAMERAD")||message.Contains("MITSPIELER");
        bool xp=OwnXp().IsMatch(message);
        // Scores express strict heuristic cue matches, not measured recognition probabilities.
        return new(at,state,state==GameState.UNKNOWN?0:.96,opening&&!teammate?.96:0,opening&&xp&&!teammate?.96:0);
    }
    [GeneratedRegex(@"\b(?:SPIELEN|PLAY)\b")] private static partial Regex PlayWord();
    [GeneratedRegex(@"\b(?:BEREIT|READY)\b")] private static partial Regex ReadyWord();
    [GeneratedRegex(@"\bBATTLE\b.{0,20}\bR[O0][YV]ALE\b")] private static partial Regex BattleRoyale();
    [GeneratedRegex(@"\b(?:TRUHE|CHEST)\b")] private static partial Regex ChestWord();
    [GeneratedRegex(@"\b\d{1,2}\s*:\s*\d{2}\b")] private static partial Regex Clock();
    [GeneratedRegex(@"\+\s*\d[\d\s.,]*\s*(?:EP|XP)\b")] private static partial Regex OwnXp();
}
