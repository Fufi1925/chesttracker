using ChestTracker.Core;
using Xunit;
namespace ChestTracker.Tests;
public sealed class OverlayToggleTests
{
    [Fact]public void TypingInLoginDoesNotToggle(){var key=new OverlayToggle();Assert.False(key.Update(true,true));Assert.False(key.Update(false,true));Assert.False(key.Update(true,true));}
    [Fact]public void HeldKeyTogglesOnlyOnce(){var key=new OverlayToggle();Assert.True(key.Update(true,false));Assert.False(key.Update(true,false));Assert.False(key.Update(false,false));Assert.True(key.Update(true,false));}
    [Fact]public void FocusChangeWhileHeldDoesNotToggle(){var key=new OverlayToggle();Assert.False(key.Update(true,true));Assert.False(key.Update(true,false));Assert.False(key.Update(false,false));Assert.True(key.Update(true,false));}
}
