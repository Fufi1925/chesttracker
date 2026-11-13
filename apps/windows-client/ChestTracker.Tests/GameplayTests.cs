using ChestTracker.Core;
using Xunit;
namespace ChestTracker.Tests;

public sealed class GameplayTests
{
    private readonly GameplayAnalyzer analyzer = new();
    private readonly ChestDetector detector = new("test", TimeSpan.FromSeconds(2));
    private DateTimeOffset at = DateTimeOffset.UnixEpoch.AddDays(1);
    private DetectorUpdate Frame(bool harvest = false, double sound = .95, int loot = 0, int fresh = 0, double label = 0, bool xp = false, HudText? text = null)
    {
        at = at.AddMilliseconds(167);
        return detector.Process(analyzer.Analyze(at, new(true, sound, harvest, loot, fresh, label, xp, GoldChestLabelScore: label, GoldXpScore: xp ? .9 : 0), text ?? new()).Observation);
    }
    private void Start() { for (int i = 0; i < 5; i++) Frame(); }
    private void Harvest() { for (int i = 0; i < 8; i++) Frame(harvest: true); }
    [Fact] public void WalkingPastLootAndSoundDoesNotCount() { Start(); for (int i = 0; i < 40; i++) Frame(loot: 1200, fresh: 800); Assert.Equal(0, detector.Count); }
    [Fact] public void ExistingLootNextToHarvestDoesNotCount() { Start(); Harvest(); for (int i = 0; i < 15; i++) Frame(loot: 1200); Assert.Equal(0, detector.Count); }
    [Fact] public void OnePickaxeFlashAfterHarvestDoesNotCount() { Start(); Harvest(); Frame(); Frame(); Frame(loot: 1500, fresh: 1000); for (int i = 0; i < 15; i++) Frame(); Assert.Equal(0, detector.Count); }
    [Fact] public void NewlyReleasedLootAfterHarvestAndSoundCountsOnce()
    {
        Start(); Harvest(); Frame(); Frame(); Frame(loot: 1100, fresh: 500); Frame(loot: 900, fresh: 400);
        for (int i = 0; i < 20; i++) Frame(loot: 900);
        Assert.Equal(1, detector.Count);
    }
    [Fact] public void LootReleaseWithoutRecentChestSoundDoesNotCount()
    {
        Start(); for (int i = 0; i < 12; i++) Frame(harvest: true, sound: 0);
        for (int i = 0; i < 15; i++) Frame(sound: 0, loot: 900, fresh: 500);
        Assert.Equal(0, detector.Count);
    }
    [Fact] public void PauseClearsHarvestHistory() { Start(); Harvest(); analyzer.Reset(); for (int i = 0; i < 15; i++) Frame(loot: 900, fresh: 500); Assert.Equal(0, detector.Count); }
    [Fact] public void OwnChestXpLabelCountsWithoutSearchedWording() { Start(); Frame(text: new(Notification: "+600 EP TRUHE")); for (int i = 0; i < 30; i++) Frame(); Assert.Equal(1, detector.Count); }
    [Fact] public void OtherXpMessagesDoNotCount() { Start(); for (int i = 0; i < 30; i++) Frame(text: new(Notification: "+900 EP NEUE STURMPHASE")); Assert.Equal(0, detector.Count); }
    [Fact] public void VisualXpLabelNeedsItsOwnXpLine() { Start(); for (int i = 0; i < 30; i++) Frame(label: .99); Assert.Equal(0, detector.Count); }
    [Fact] public void WeakPurpleScenePatternDoesNotCount()
    {
        Start(); for (int i = 0; i < 30; i++) Frame(label: .65, xp: true); Assert.Equal(0, detector.Count);
    }
    [Fact] public void OneConfirmedVisualLabelSurvivesShortDisplayAndDoesNotDuplicate()
    {
        Start(); Frame(label: .9, xp: true); Frame(label: .9, xp: true); for (int i = 0; i < 15; i++) Frame();
        Assert.Equal(1, detector.Count);
    }
    [Fact] public void ResultScreenCompletesMatchAndSpectatorHudCannotStartAnother()
    {
        Start(); Frame(text: new(Notification: "+600 EP TRUHE")); Frame(); var match = detector.MatchId;
        DetectorUpdate? ended = null;
        for (int i = 0; i < 4; i++) { var update = Frame(text: new(Loading: "DEINE PLATZIERUNG IST #35")); if (update.Match is not null) ended = update; }
        Assert.Equal(match, ended!.Match!.Id); Assert.Equal("COMPLETED", ended.Match.Status);
        for (int i = 0; i < 40; i++) Frame(); Assert.Null(detector.MatchId); Assert.Equal(1, detector.Count);
    }
    [Fact] public void BlackOrWrongSizeImagesCannotPretendToBeGameplay()
    {
        var pixels = new PixelHudDetector();
        Assert.Throws<ArgumentException>(() => pixels.Analyze(at, new byte[4]));
        var result = pixels.Analyze(at, new byte[960 * 540 * 4]);
        Assert.False(result.GameplayHud); Assert.False(result.OwnXpLabel); Assert.Equal(0, result.ChestSoundScore);
    }
    [Fact] public void PersistentOwnXpBannerDoesNotCountAgainAfterCooldown()
    {
        Start(); for (int i = 0; i < 80; i++) Frame(label: .9, xp: true); Assert.Equal(1, detector.Count);
    }
    [Fact] public void GoldenLabelNeedsAnIndependentXpSignature()
    {
        var d = new GameplayAnalyzer();
        for (int i = 0; i < 12; i++)
        {
            var r = d.Analyze(at.AddMilliseconds(i * 167), new(true, .95, false, 0, 0, 0, false, true, .95, .2), new());
            Assert.Null(r.ChestReason);
        }
    }
    [Fact] public void SingleChestPatternFlashDoesNotBecomeAnOpening()
    {
        Start(); Frame(label: .95, xp: true); for (int i = 0; i < 20; i++) Frame(); Assert.Equal(0, detector.Count);
    }
    [Fact] public void GoldenChestLabelPlusXpSignatureProducesOpeningEvidence()
    {
        var d = new GameplayAnalyzer();
        var first = d.Analyze(at, new(true, .95, false, 0, 0, 0, false, true, .95, .9), new());
        Assert.Null(first.ChestReason);
        var r = d.Analyze(at.AddMilliseconds(167), new(true, .95, false, 0, 0, 0, false, true, .95, .9), new());
        Assert.NotNull(r.ChestReason); Assert.True(r.Observation.OwnPlayerScore >= .94);
    }

    [Fact] public void OwnContainerLootMessageConfirmsHarvestWithoutRarityColorGuess()
    {
        Start(); Harvest(); Frame(text: new(Container: "+19 BEHÄLTER GEÖFFNET")); Frame(); Assert.Equal(1, detector.Count);
    }
    [Fact] public void StrongOwnContainerFeedbackCanConfirmAShortHarvest()
    {
        Start(); Frame(harvest: true); Frame(harvest: true); Frame(text: new(Container: "+19 BEHÄLTER GEÖFFNET")); Frame(); Assert.Equal(1, detector.Count);
    }
    [Fact] public void RemoteContainerFeedbackWithoutOwnHarvestDoesNotCount()
    {
        Start(); for (int i = 0; i < 15; i++) Frame(text: new(Container: "+19 BEHÄLTER GEÖFFNET")); Assert.Equal(0, detector.Count);
    }
    [Fact] public void ExistingContainerBannerCannotTurnLaterEffectsIntoAnotherOpening()
    {
        Start(); Frame(text: new(Container: "+19 BEHÄLTER GEÖFFNET"));
        for (int i = 0; i < 10; i++) Frame(harvest: true, text: new(Container: "+19 BEHÄLTER GEÖFFNET"));
        for (int i = 0; i < 15; i++) Frame(loot: 1100, fresh: 400, text: new(Container: "+19 BEHÄLTER GEÖFFNET"));
        Assert.Equal(0, detector.Count);
    }
    [Fact] public void UnrelatedPetXpDoesNotConfirmAContainer()
    {
        Assert.False(ContainerCue.Read("+50 GEGNER ELIMINIERT").Opened);
        Assert.True(ContainerCue.Read("+14 BEHBLTER GEOFFRET").Opened);
        Assert.True(ContainerCue.Read("+19 BEHALTER\nGEOFFNET").Opened);
    }

    [Fact] public void AContainerBadgeForAnAlreadyCountedNormalOpeningDoesNotCountAgain()
    {
        Start(); Frame(text: new(Notification: "+600 EP TRUHE"));
        for (int i = 0; i < 25; i++) Frame(harvest: true, text: new(Notification: "+600 EP TRUHE", Container: "+19 BEHÄLTER GEÖFFNET"));
        for (int i = 0; i < 10; i++) Frame(text: new(Container: "+19 BEHÄLTER GEÖFFNET"));
        Assert.Equal(1, detector.Count);
    }

}
