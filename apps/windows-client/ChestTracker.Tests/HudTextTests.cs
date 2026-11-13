using ChestTracker.Core;
using Xunit;
namespace ChestTracker.Tests;
public sealed class HudTextTests
{
 [Fact]public void ExplicitOwnChestNotificationHasBothCues(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"STURM 1:30","","TRUHE DURCHSUCHT + 100 EP","");Assert.Equal(GameState.IN_MATCH,o.State);Assert.True(o.NotificationScore>.94);Assert.True(o.OwnPlayerScore>.94);}
 [Fact]public void SeeingAChestWithoutOpeningDoesNotCount(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"STURM 1:30","","TRUHE","");Assert.Equal(0,o.NotificationScore);Assert.Equal(0,o.OwnPlayerScore);}
 [Fact]public void OpeningWithoutOwnXpIsNotEnough(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"STORM 0:45","","CHEST SEARCHED","");Assert.Equal(0,o.OwnPlayerScore);}
 [Fact]public void TeamMateCueIsRejected(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"STURM 1:30","","TEAMKAMERAD TRUHE DURCHSUCHT + 100 EP","");Assert.Equal(0,o.NotificationScore);Assert.Equal(0,o.OwnPlayerScore);}
 [Fact]public void LobbyNeedsTwoCues(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"","SPIELEN BEREIT","","");Assert.Equal(GameState.LOBBY,o.State);}
 [Fact]public void ContradictoryHudRemainsUnknown(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"STURM 1:30","SPIELEN BEREIT","","");Assert.Equal(GameState.UNKNOWN,o.State);}
 [Fact]public void ArbitraryVisibleWindowDoesNotCreateAMatch(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"Fortnite","Fortnite","","");Assert.Equal(GameState.UNKNOWN,o.State);}
 [Fact]public void EnglishAndSplitXpAreAccepted(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"STORM 0:45","","CHEST SEARCHED\n+ 100 XP","");Assert.True(o.OwnPlayerScore>.94);}
 [Fact]public void ActualShortGermanXpNotificationIsAccepted(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"","","+600 EP TRUHE","",true);Assert.Equal(GameState.IN_MATCH,o.State);Assert.True(o.OwnPlayerScore>.94);}
 [Fact]public void CurrentLobbyPanelIsAccepted(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"","BATTLE\nROYALE SPIELEN","","");Assert.Equal(GameState.LOBBY,o.State);}
 [Fact]public void CurrentHudDoesNotNeedStormWordWhenVisualHudIsPresent(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"0:53 78","","","",true);Assert.Equal(GameState.IN_MATCH,o.State);}
 [Fact]public void ResultOverridesSpectatedGameplayHud(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"0:53 78","","","DEINE PLATZIERUNG IST #35",true);Assert.Equal(GameState.MATCH_END_PENDING,o.State);}
 [Fact]public void MinimapAndClockCanRecoverPartiallyHiddenHealthHud(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"0:53 78","","","",false,true);Assert.Equal(GameState.IN_MATCH,o.State);}
 [Fact]public void MinimapWithoutClockDoesNotInventMatch(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"Fortnite","","","",false,true);Assert.Equal(GameState.UNKNOWN,o.State);}
 [Fact]public void VictoryClosesTheRound(){var o=HudTextDetector.Analyze(DateTimeOffset.UtcNow,"","","","#1 EPISCHER SIEG",true);Assert.Equal(GameState.MATCH_END_PENDING,o.State);}

}
