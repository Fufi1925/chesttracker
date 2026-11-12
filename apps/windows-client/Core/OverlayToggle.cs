namespace ChestTracker.Core;

/// <summary>One toggle per key press. Typing in the client's own fields never toggles the overlay.</summary>
public sealed class OverlayToggle
{
    private bool wasDown;
    public bool Update(bool isDown, bool clientHasFocus)
    {
        var pressed = isDown && !wasDown;
        wasDown = isDown;
        return pressed && !clientHasFocus;
    }
    public void Reset() => wasDown = false;
}
