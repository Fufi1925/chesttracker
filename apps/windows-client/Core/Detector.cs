namespace ChestTracker.Core;

public enum GameState { OFFLINE, UNKNOWN, LOBBY, LOADING, IN_MATCH, MATCH_END_PENDING }
public record Observation(DateTimeOffset At, GameState State, double StateConfidence, double NotificationScore, double OwnPlayerScore);
public record ChestDetection(Guid ClientEventId, Guid MatchId, DateTimeOffset OccurredAt, double Confidence, string DetectorVersion);
public record MatchChange(Guid Id, DateTimeOffset StartedAt, DateTimeOffset? EndedAt, string Status);
public interface IChestDetector { DetectorUpdate Process(Observation frame); }
public record DetectorUpdate(GameState State, Guid? MatchId, int Count, ChestDetection? Event, MatchChange? Match);

/// <summary>Conservative state machine. A notification alone cannot count: an independent own-player cue is required.</summary>
public sealed class ChestDetector : IChestDetector
{
    public GameState State { get; private set; } = GameState.UNKNOWN;
    public Guid? MatchId { get; private set; }
    public int Count { get; private set; }
    private GameState candidate = GameState.UNKNOWN;
    private int stableFrames, positiveFrames, absentFrames;
    private bool notificationLatched;
    private DateTimeOffset lastFrame, lastEvent, startedAt;
    public DetectorUpdate Process(Observation frame)
    {
        if (frame.At <= lastFrame) return new(State, MatchId, Count, null, null);
        if (lastFrame != default && frame.At - lastFrame > TimeSpan.FromSeconds(3))
        { State = GameState.UNKNOWN; stableFrames = positiveFrames = absentFrames = 0; }
        lastFrame = frame.At;
        MatchChange? change = null;
        ChestDetection? detection = null;
        var observed = frame.StateConfidence >= .92 ? frame.State : GameState.UNKNOWN;
        if (candidate == observed) stableFrames++; else { candidate = observed; stableFrames = 1; }
        if (observed == GameState.OFFLINE) State = GameState.OFFLINE;
        else if (observed == GameState.UNKNOWN && stableFrames >= 3) State = GameState.UNKNOWN;
        else if (observed == GameState.LOADING && stableFrames >= 3) State = GameState.LOADING;
        else if (observed == GameState.IN_MATCH && stableFrames >= 4)
        {
            State = GameState.IN_MATCH;
            if (MatchId is null)
            {
                MatchId = Guid.NewGuid(); startedAt = frame.At; Count = 0;
                notificationLatched = false; positiveFrames = absentFrames = 0;
                change = new(MatchId.Value, startedAt, null, "ACTIVE");
            }
        }
        else if (observed == GameState.LOBBY)
        {
            if (MatchId is not null && stableFrames < 12) State = GameState.MATCH_END_PENDING;
            if (stableFrames >= 12)
            {
                if (MatchId is not null) change = new(MatchId.Value, startedAt, frame.At, "COMPLETED");
                MatchId = null; State = GameState.LOBBY; Count = 0;
                notificationLatched = false; positiveFrames = absentFrames = 0;
            }
        }
        if (State == GameState.IN_MATCH && observed == GameState.IN_MATCH && MatchId is not null)
        {
            bool positive = frame.NotificationScore >= .94 && frame.OwnPlayerScore >= .94;
            if (positive) { positiveFrames++; absentFrames = 0; }
            else { positiveFrames = 0; absentFrames++; if (absentFrames >= 3) notificationLatched = false; }
            if (positiveFrames >= 2 && !notificationLatched && frame.At-lastEvent >= TimeSpan.FromMilliseconds(800))
            {
                notificationLatched = true; lastEvent = frame.At; Count++;
                detection = new(Guid.NewGuid(), MatchId.Value, frame.At, Math.Min(frame.NotificationScore,frame.OwnPlayerScore), "template-replay-unvalidated-v1");
            }
        }
        else positiveFrames = 0;
        return new(State, MatchId, Count, detection, change);
    }
    public MatchChange? MarkIncomplete(DateTimeOffset at)
    {
        if (MatchId is null) return null;
        var change = new MatchChange(MatchId.Value,startedAt,at,"INCOMPLETE");
        MatchId=null; State=GameState.UNKNOWN; Count=0; return change;
    }
}
