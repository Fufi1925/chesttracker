namespace ChestTracker.Core;

public record HudText(string Gameplay = "", string Lobby = "", string Notification = "", string Loading = "");
public record AnalysisResult(Observation Observation, string Detail, string? ChestReason);

/// <summary>Combines current HUD evidence with a short local history. Never treats existing loot or a sound cue alone as a chest.</summary>
public sealed class GameplayAnalyzer
{
    private DateTimeOffset lastFrame, soundAt, harvestStarted, harvestAt, absentSince, pulseUntil, lastConfirmation;
    private int harvestFrames, releaseFrames;
    private bool harvestArmed;
    private string? pulseReason;
    public void Reset()
    {
        lastFrame = soundAt = harvestStarted = harvestAt = absentSince = pulseUntil = lastConfirmation = default;
        harvestFrames = releaseFrames = 0; harvestArmed = false; pulseReason = null;
    }
    public AnalysisResult Analyze(DateTimeOffset at, PixelEvidence pixels, HudText text)
    {
        if (lastFrame != default && (at <= lastFrame || at - lastFrame > TimeSpan.FromSeconds(1))) Reset();
        lastFrame = at;
        var observation = HudTextDetector.Analyze(at, text.Gameplay, text.Lobby, text.Notification, text.Loading, pixels.GameplayHud);
        if (observation.State != GameState.IN_MATCH)
        {
            harvestArmed = false; harvestFrames = releaseFrames = 0; harvestStarted = harvestAt = absentSince = pulseUntil = default;
            return new(observation, observation.State switch { GameState.LOBBY => "Lobby-Hinweise erkannt", GameState.LOADING => "Ladebildschirm erkannt", GameState.MATCH_END_PENDING => "Ergebnisbildschirm erkannt", _ => "Spielbilder empfangen · HUD noch unklar" }, null);
        }
        if (pixels.ChestSoundScore >= .83) soundAt = at;
        // The small, specific TRUHE label plus its own purple XP line is a local opening notification.
        // No world-space chest/loot template can trigger this path.
        bool ownText = observation.NotificationScore >= .94 && observation.OwnPlayerScore >= .94;
        bool ownLabel = pixels.ChestLabelScore >= .64 && pixels.OwnXpLabel;
        if (ownText || ownLabel) Confirm(at, "Eigene Truhen-EP-Meldung");
        if (pixels.HarvestTarget)
        {
            if (harvestAt == default || at - harvestAt > TimeSpan.FromMilliseconds(450)) { harvestStarted = at; harvestFrames = 0; }
            harvestAt = at; harvestFrames++; absentSince = default; releaseFrames = 0;
            if (harvestFrames >= 3 && at - harvestStarted >= TimeSpan.FromMilliseconds(500)) harvestArmed = true;
        }
        else
        {
            if (absentSince == default) absentSince = at;
            bool released = harvestArmed && at - harvestAt <= TimeSpan.FromMilliseconds(1600) &&
                at - absentSince >= TimeSpan.FromMilliseconds(250) && at - soundAt <= TimeSpan.FromMilliseconds(1500);
            if (released && pixels.LootPixels >= 300 && pixels.NewLootPixels >= 40) releaseFrames++;
            else if (!released || pixels.LootPixels < 250) releaseFrames = 0;
            if (releaseFrames >= 2) { Confirm(at, "Abbau mit neuem Loot und Truhensymbol"); harvestArmed = false; releaseFrames = 0; }
            if (harvestAt != default && at - harvestAt > TimeSpan.FromMilliseconds(1600)) { harvestArmed = false; harvestFrames = 0; }
        }
        bool confirmed = at <= pulseUntil && pulseUntil != default;
        observation = observation with { NotificationScore = confirmed ? .96 : 0, OwnPlayerScore = confirmed ? .96 : 0 };
        string detail = confirmed ? pulseReason! : pixels.HarvestTarget ? "HUD erkannt · Abbau wird geprüft" : pixels.ChestSoundScore >= .83 ? "HUD erkannt · Truhe in der Nähe · noch keine Öffnung" : "HUD erkannt · Match aktiv";
        return new(observation, detail, confirmed ? pulseReason : null);
    }
    private void Confirm(DateTimeOffset at, string reason)
    {
        if (lastConfirmation != default && at - lastConfirmation < TimeSpan.FromSeconds(4)) return;
        lastConfirmation = at; pulseUntil = at.AddMilliseconds(650); pulseReason = reason;
    }
}
