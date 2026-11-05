using Ghostty.Vt.Enums;
using Xunit;

namespace Ghostty.Vt.Tests;

public class SetterResultTests
{
    [Fact]
    public void KeyEvent_InvalidKey_ThrowsAndKeepsPreviousValue()
    {
        using var keyEvent = new KeyEvent { Key = (int)GhosttyKey.A };

        Assert.Throws<GhosttyException>(() => keyEvent.Key = 200);
        Assert.Equal((int)GhosttyKey.A, keyEvent.Key);
    }

    [Fact]
    public void KeyEvent_InvalidAction_ThrowsAndKeepsPreviousValue()
    {
        using var keyEvent = new KeyEvent { Action = 1 };

        Assert.Throws<GhosttyException>(() => keyEvent.Action = 7);
        Assert.Equal(1, keyEvent.Action);
    }

    [Fact]
    public void MouseEvent_InvalidAction_ThrowsAndKeepsPreviousValue()
    {
        using var mouseEvent = new MouseEvent { Action = 0 };

        Assert.Throws<GhosttyException>(() => mouseEvent.Action = 99);
        Assert.Equal(0, mouseEvent.Action);
    }

    [Fact]
    public void MouseEvent_InvalidButton_ThrowsAndKeepsPreviousValue()
    {
        using var mouseEvent = new MouseEvent();

        Assert.Throws<GhosttyException>(() => mouseEvent.Button = 42);
        Assert.Equal(-1, mouseEvent.Button);
    }

    [Fact]
    public void MouseEncoder_InvalidSize_Throws()
    {
        using var encoder = new MouseEncoder();

        Assert.Throws<GhosttyException>(
            () => encoder.SetSize(screenWidth: 640, screenHeight: 384, cellWidth: 0, cellHeight: 0));
    }
}
