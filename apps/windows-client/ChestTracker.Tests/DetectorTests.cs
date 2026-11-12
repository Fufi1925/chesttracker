using ChestTracker.Core;
using Xunit;
namespace ChestTracker.Tests;
public sealed class DetectorTests
{
 private readonly ChestDetector d=new();private DateTimeOffset at=DateTimeOffset.UtcNow;
 private DetectorUpdate Frame(GameState state=GameState.IN_MATCH,double chest=0,double own=0,double certainty=.99){at=at.AddMilliseconds(83);return d.Process(new(at,state,certainty,chest,own));}
 private void Start(){for(int i=0;i<5;i++)Frame();}
 [Fact]public void WalkingPastChestNeverCounts(){Start();for(int i=0;i<100;i++)Frame(chest:.3,own:1);Assert.Equal(0,d.Count);}
 [Fact]public void OpeningNeedsTwoOwnNotificationFrames(){Start();Assert.Null(Frame(chest:.99,own:.99).Event);Assert.NotNull(Frame(chest:.99,own:.99).Event);Assert.Equal(1,d.Count);}
 [Fact]public void PersistentNotificationCountsOnlyOnce(){Start();for(int i=0;i<100;i++)Frame(chest:1,own:1);Assert.Equal(1,d.Count);}
 [Fact]public void TeamMateDoesNotCount(){Start();for(int i=0;i<30;i++)Frame(chest:1,own:.2);Assert.Equal(0,d.Count);}
 [Fact]public void QuickSeparateNotificationsCanCount(){Start();Frame(chest:1,own:1);Frame(chest:1,own:1);for(int i=0;i<10;i++)Frame();Frame(chest:1,own:1);Frame(chest:1,own:1);Assert.Equal(2,d.Count);}
 [Fact]public void InvisibleNotificationDoesNotCount(){Start();for(int i=0;i<20;i++)Frame(chest:0,own:1);Assert.Equal(0,d.Count);}
 [Fact]public void LobbyRequiresHysteresisAndResetsCount(){Start();var id=d.MatchId;Frame(chest:1,own:1);Frame(chest:1,own:1);Frame(GameState.LOBBY);Assert.Equal(id,d.MatchId);for(int i=0;i<11;i++)Frame(GameState.LOBBY);Assert.Null(d.MatchId);Assert.Equal(0,d.Count);Assert.Equal(GameState.LOBBY,d.State);}
 [Fact]public void LoadingDoesNotFinishMatch(){Start();var id=d.MatchId;for(int i=0;i<20;i++)Frame(GameState.LOADING);Assert.Equal(id,d.MatchId);Start();Assert.Equal(id,d.MatchId);}
 [Fact]public void MatchSwitchStartsAtZero(){Start();var id=d.MatchId;for(int i=0;i<12;i++)Frame(GameState.LOBBY);Start();Assert.NotEqual(id,d.MatchId);Assert.Equal(0,d.Count);}
 [Fact]public void DisconnectDoesNotInventEnd(){Start();var id=d.MatchId;Frame(GameState.OFFLINE);Assert.Equal(id,d.MatchId);Assert.Equal(GameState.OFFLINE,d.State);}
 [Fact]public void AmbiguousFramesDoNotStartMatch(){for(int i=0;i<50;i++)Frame(certainty:.5);Assert.Null(d.MatchId);}
 [Fact]public void DuplicateFrameCannotDuplicateEvent(){Start();var observation=new Observation(at.AddMilliseconds(83),GameState.IN_MATCH,1,1,1);d.Process(observation);d.Process(observation);Assert.Equal(0,d.Count);}
 [Fact]public void StopMarksIncompleteOnlyWhenAMatchExists(){Assert.Null(d.MarkIncomplete(at));Start();Assert.Equal("INCOMPLETE",d.MarkIncomplete(at)!.Status);Assert.Null(d.MatchId);}
}
