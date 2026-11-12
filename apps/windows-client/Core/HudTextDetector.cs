using System.Text;
using System.Text.RegularExpressions;
namespace ChestTracker.Core;
public static partial class HudTextDetector
{
    private static string Normalize(string value) => string.Join(' ', value.Normalize(NormalizationForm.FormKC).ToUpperInvariant().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
    public static Observation Analyze(DateTimeOffset at, string gameplayHud, string lobbyHud, string notificationHud, string loadingHud)
    {
        var game=Normalize(gameplayHud);var lobby=Normalize(lobbyHud);var message=Normalize(notificationHud);var loading=Normalize(loadingHud);
        bool lobbyCue=(lobby.Contains("SPIELEN")||lobby.Contains("PLAY"))&&(lobby.Contains("BEREIT")||lobby.Contains("READY")||lobby.Contains("BATTLE ROYALE"));
        bool matchCue=(game.Contains("STURM")||game.Contains("STORM"))&&Clock().IsMatch(game);
        var state=lobbyCue&&!matchCue?GameState.LOBBY:matchCue&&!lobbyCue?GameState.IN_MATCH:
            loading.Contains("VERBINDE")||loading.Contains("CONNECTING")||loading.Contains("LADEVORGANG")?GameState.LOADING:GameState.UNKNOWN;
        bool opening=message.Contains("TRUHE DURCHSUCHT")||message.Contains("TRUHEN DURCHSUCHT")||message.Contains("CHEST SEARCHED")||message.Contains("SEARCHED CHEST");
        bool teammate=message.Contains("TEAMMATE")||message.Contains("TEAMKAMERAD")||message.Contains("MITSPIELER");
        bool xp=OwnXp().IsMatch(message);
        // Scores express strict heuristic cue matches, not measured recognition probabilities.
        return new(at,state,state==GameState.UNKNOWN?0:.96,opening&&!teammate?.96:0,opening&&xp&&!teammate?.96:0);
    }
    [GeneratedRegex(@"\b\d{1,2}\s*:\s*\d{2}\b")] private static partial Regex Clock();
    [GeneratedRegex(@"\+\s*\d[\d\s.,]*\s*(?:EP|XP)\b")] private static partial Regex OwnXp();
}
