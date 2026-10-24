namespace Lss.Tests;

public class SessionTrackerTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 18, 0, 0);
    private static readonly DateTime ProcessStart = new(2026, 10, 24, 17, 30, 0);

    [Fact]
    public void Start_UsesTheProcessStartTime()
    {
        var tracker = new SessionTracker();

        SessionEvent e = tracker.Update(true, "Mario", () => ProcessStart, Now);

        Assert.Equal(SessionChange.Started, e.Change);
        Assert.Equal("Mario", e.Profile);
        Assert.Equal(ProcessStart, tracker.Start);
        Assert.True(tracker.Running);
    }

    [Fact]
    public void Start_WithoutProcessTime_UsesNow()
    {
        var tracker = new SessionTracker();

        SessionEvent e = tracker.Update(true, "Mario", () => null, Now);

        Assert.Equal(Now, e.Start);
        Assert.Equal(Now, tracker.Start);
    }

    [Fact]
    public void End_ReturnsProfileAndTimes()
    {
        var tracker = new SessionTracker();
        tracker.Update(true, "Mario", () => ProcessStart, Now);

        SessionEvent e = tracker.Update(false, "", () => null, Now.AddHours(1));

        Assert.Equal(SessionChange.Ended, e.Change);
        Assert.Equal("Mario", e.Profile);
        Assert.Equal(ProcessStart, e.Start);
        Assert.Equal(Now.AddHours(1), e.End);
        Assert.False(tracker.Running);
        Assert.Null(tracker.Start);
        Assert.Equal("", tracker.Profile);
    }

    [Fact]
    public void RepeatedCalls_ReturnNone()
    {
        var tracker = new SessionTracker();

        tracker.Update(true, "Mario", () => ProcessStart, Now);

        Assert.Equal(SessionChange.None, tracker.Update(true, "Mario", () => null, Now).Change);

        tracker.Update(false, "", () => null, Now);

        Assert.Equal(SessionChange.None, tracker.Update(false, "", () => null, Now).Change);
    }

    [Fact]
    public void EndNow_WithOpenSession_ReturnsEnded()
    {
        var tracker = new SessionTracker();
        tracker.Update(true, "Mario", () => ProcessStart, Now);

        SessionEvent e = tracker.EndNow(Now.AddMinutes(5));

        Assert.Equal(SessionChange.Ended, e.Change);
        Assert.Equal("Mario", e.Profile);
        Assert.False(tracker.Running);
    }

    [Fact]
    public void EndNow_WithoutSession_ReturnsNone()
    {
        var tracker = new SessionTracker();

        Assert.Equal(SessionChange.None, tracker.EndNow(Now).Change);
    }
}
