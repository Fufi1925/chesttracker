namespace ChestTracker.Core;

public record HudText(string Gameplay = "", string Lobby = "", string Notification = "", string Loading = "", string Container = "");
public record AnalysisResult(Observation Observation, string Detail, string? ChestReason);

/// <summary>Combines current HUD evidence with a short local history. Never treats existing loot or a sound cue alone as a chest.</summary>
public sealed class GameplayAnalyzer
{
    private DateTimeOffset lastFrame, soundAt, harvestStarted, harvestAt, absentSince, pulseUntil, lastConfirmation;
    private int harvestFrames;
    private bool harvestArmed, labelLatched, containerSupported, containerLatched;
    private int containerAmount, labelFrames;
    private DateTimeOffset lastContainerAt, lastStrongLabelAt;
    private DateTimeOffset lastLabelAt, lootFreshAt, lootAt;
    private int lootFrames;
    private bool containerVisible;
    private DateTimeOffset containerWeakAt, pendingContainerAt;
    private string? pulseReason;
    public void Reset()
    {
        lastFrame = soundAt = harvestStarted = harvestAt = absentSince = pulseUntil = lastConfirmation = default;
        harvestFrames = 0; harvestArmed = labelLatched = false; lastLabelAt = lastContainerAt = lootFreshAt = lootAt = default; lootFrames = 0; containerAmount = labelFrames = 0; lastStrongLabelAt = default; containerSupported = containerLatched = containerVisible = false; containerWeakAt = pendingContainerAt = default; pulseReason = null;
    }
    public AnalysisResult Analyze(DateTimeOffset at, PixelEvidence pixels, HudText text)
    {
        if (lastFrame != default && (at <= lastFrame || at - lastFrame > TimeSpan.FromSeconds(1))) Reset();
        lastFrame = at;
        var observation = HudTextDetector.Analyze(at, text.Gameplay, text.Lobby, text.Notification, text.Loading, pixels.GameplayHud, pixels.MapHud);
        if (observation.State != GameState.IN_MATCH)
        {
            labelLatched = false; labelFrames = 0; lastStrongLabelAt = default; lastLabelAt = default; harvestArmed = false; harvestFrames = 0; harvestStarted = harvestAt = absentSince = pulseUntil = default;
            return new(observation, observation.State switch { GameState.LOBBY => "Lobby-Hinweise erkannt", GameState.LOADING => "Ladebildschirm erkannt", GameState.MATCH_END_PENDING => "Ergebnisbildschirm erkannt", _ => "Spielbilder empfangen · HUD noch unklar" }, null);
        }
        if (pixels.ChestSoundScore >= .83) soundAt = at;
        var container = ContainerCue.Read(text.Container);
        bool newContainerFlash = false;
        if (pixels.ContainerSignal >= .55)
        { newContainerFlash = !containerVisible; containerVisible = true; containerWeakAt = default; }
        else if (pixels.ContainerSignal < .30)
        {
            if (containerWeakAt == default) containerWeakAt = at;
            if (at - containerWeakAt >= TimeSpan.FromMilliseconds(300)) containerVisible = false;
        }
        bool freshContainer = container.Opened && (!containerLatched || container.Amount > containerAmount || newContainerFlash);
        if (freshContainer) pendingContainerAt = at;
        if (container.Opened) { containerSupported = containerLatched = true; containerAmount = Math.Max(containerAmount, container.Amount); lastContainerAt = at; }
        else if (at - lastContainerAt > TimeSpan.FromMilliseconds(750)) { containerLatched = false; containerAmount = 0; }
        // Require the specific chest heading and separate EP / plus glyphs for either HUD colour.
        bool ownText = observation.NotificationScore >= .94 && observation.OwnPlayerScore >= .94;
        bool strongLabel = pixels.GoldChestLabelScore >= .84 && pixels.GoldXpScore >= .70;
        if (strongLabel) { labelFrames = at - lastStrongLabelAt <= TimeSpan.FromMilliseconds(450) ? labelFrames + 1 : 1; lastStrongLabelAt = at; }
        else if (at - lastStrongLabelAt > TimeSpan.FromMilliseconds(450)) labelFrames = 0;
        bool ownLabel = labelFrames >= 2 || (labelLatched && pixels.GoldChestLabelScore >= .80);
        if (ownText || ownLabel)
        {
            if (!labelLatched) { Confirm(at, "Eigene Truhen-EP-Meldung"); harvestArmed = false; harvestFrames = 0; }
            labelLatched = true; lastLabelAt = at;
        }
        else if (at - lastLabelAt > TimeSpan.FromMilliseconds(1000)) labelLatched = false;
        if (labelLatched) { pendingContainerAt = default; harvestFrames = 0; harvestArmed = false; }
        bool boundedLoot = pixels.LootPixels is >= 300 and <= 6000;
        if (harvestFrames >= 2 && at - harvestAt <= TimeSpan.FromMilliseconds(2600) && at - soundAt <= TimeSpan.FromMilliseconds(2000) && boundedLoot)
        {
            if (pixels.NewLootPixels >= 40) lootFreshAt = at;
            if (lootFreshAt != default && at - lootFreshAt <= TimeSpan.FromMilliseconds(1000)) { lootFrames++; lootAt = at; }
        }
        else if (!boundedLoot || at - lootAt > TimeSpan.FromMilliseconds(350)) lootFrames = 0;
        if (pendingContainerAt != default && pendingContainerAt >= harvestStarted && at - pendingContainerAt <= TimeSpan.FromMilliseconds(1200) && !labelLatched && harvestFrames >= 2 && at - harvestAt <= TimeSpan.FromMilliseconds(2600) && at - soundAt <= TimeSpan.FromMilliseconds(2000))
        { pendingContainerAt = default; Confirm(at, "Eigener Abbau mit neuer Behälter-Lootmeldung"); harvestArmed = false; harvestFrames = lootFrames = 0; }
        if (pixels.HarvestTarget)
        {
            if (harvestAt == default || at - harvestAt > TimeSpan.FromMilliseconds(450)) { harvestStarted = at; harvestFrames = lootFrames = 0; lootFreshAt = lootAt = default; }
            harvestAt = at; harvestFrames++; absentSince = default;
            if (harvestFrames >= 3 && at - harvestStarted >= TimeSpan.FromMilliseconds(500)) harvestArmed = true;
        }
        else
        {
            if (absentSince == default) absentSince = at;
            bool covered = ContainerCue.FeedbackCovered(text.Container);
            bool released = (!containerSupported || covered) && harvestArmed && at - harvestAt <= TimeSpan.FromMilliseconds(covered ? 2600 : 1600) &&
                at - absentSince >= TimeSpan.FromMilliseconds(250) && at - soundAt <= TimeSpan.FromMilliseconds(1500);
            if (released && lootFrames >= 2 && at - lootAt <= TimeSpan.FromMilliseconds(500) && at - lootFreshAt <= TimeSpan.FromMilliseconds(1000))
            { Confirm(at, "Abbau mit neuem Loot und Truhensymbol"); harvestArmed = false; harvestFrames = lootFrames = 0; }
            if (harvestAt != default && at - harvestAt > TimeSpan.FromMilliseconds(2600)) { harvestArmed = false; harvestFrames = 0; }
        }
        bool confirmed = at <= pulseUntil && pulseUntil != default;
        observation = observation with { NotificationScore = confirmed ? .96 : 0, OwnPlayerScore = confirmed ? .96 : 0 };
        string detail = confirmed ? pulseReason! : pixels.HarvestTarget ? "HUD erkannt · Abbau wird geprüft" : pixels.ChestSoundScore >= .83 ? "HUD erkannt · Truhe in der Nähe · noch keine Öffnung" : "HUD erkannt · Match aktiv";
        return new(observation, detail, confirmed ? pulseReason : null);
    }
    private void Confirm(DateTimeOffset at, string reason)
    {
        if (lastConfirmation != default && at - lastConfirmation < TimeSpan.FromMilliseconds(800)) return;
        lastConfirmation = at; pulseUntil = at.AddMilliseconds(650); pulseReason = reason;
    }
}
