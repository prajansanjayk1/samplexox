using QuietoraFocus.Core.Models;
using QuietoraFocus.Core.Services;
using Xunit;

namespace QuietoraFocus.UnitTests;

public class SessionStateMachineTests
{
    [Fact]
    public void InitialState_IsIdle()
    {
        var sm = new FocusSessionStateMachine();
        Assert.Equal(SessionState.Idle, sm.State);
        Assert.False(sm.IsActive);
    }

    [Fact]
    public void Start_TransitionsToRunning_AndInitializesMetrics()
    {
        var sm = new FocusSessionStateMachine();
        var config = new FocusSessionConfig
        {
            TaskName = "Algorithmic Study",
            PlannedSeconds = 1500,
            Mode = SessionMode.DeepFocus
        };

        sm.Start(config);

        Assert.Equal(SessionState.Running, sm.State);
        Assert.True(sm.IsActive);
        Assert.True(sm.IsRunning);
        Assert.False(sm.IsPaused);
        Assert.Equal("Algorithmic Study", sm.CurrentConfig.TaskName);
        Assert.InRange(sm.RemainingSeconds, 1498, 1500);
    }

    [Fact]
    public void Start_WhenAlreadyActive_ThrowsInvalidOperationException()
    {
        var sm = new FocusSessionStateMachine();
        sm.Start(new FocusSessionConfig { PlannedSeconds = 1200 });

        Assert.Throws<InvalidOperationException>(() => sm.Start(new FocusSessionConfig()));
    }

    [Fact]
    public void PauseAndResume_MaintainsAccurateStates()
    {
        var sm = new FocusSessionStateMachine();
        sm.Start(new FocusSessionConfig { PlannedSeconds = 1200 });

        sm.Pause();
        Assert.Equal(SessionState.Paused, sm.State);
        Assert.True(sm.IsActive);
        Assert.True(sm.IsPaused);
        Assert.False(sm.IsRunning);

        sm.Resume();
        Assert.Equal(SessionState.Running, sm.State);
        Assert.True(sm.IsRunning);
    }

    [Fact]
    public void Stop_ReturnsAccurateSessionLog_AndReturnsToIdle()
    {
        var sm = new FocusSessionStateMachine();
        sm.Start(new FocusSessionConfig
        {
            TaskName = "Physics Revision",
            Subject = "Physics",
            PlannedSeconds = 1800,
            Mode = SessionMode.StudyMode
        });

        sm.RecordDistraction();
        sm.RecordDistraction();

        var log = sm.Stop("stopped");

        Assert.NotNull(log);
        Assert.Equal("Physics Revision", log.Task);
        Assert.Equal("Physics", log.Subject);
        Assert.Equal("StudyMode", log.Mode);
        Assert.Equal(1800, log.PlannedSeconds);
        Assert.Equal(2, log.Distractions);
        Assert.Equal("stopped", log.EndReason);
        Assert.Equal(SessionState.Idle, sm.State);
    }

    [Fact]
    public void EmergencyStop_MarksEndReasonCorrectly()
    {
        var sm = new FocusSessionStateMachine();
        sm.Start(new FocusSessionConfig { TaskName = "Emergency Test", PlannedSeconds = 600 });

        var log = sm.Stop("emergency");

        Assert.Equal("emergency", log.EndReason);
        Assert.Equal("Emergency stop", log.ResultLabel);
    }

    [Fact]
    public void Pomodoro_PhaseTransitionsWorkAsExpected()
    {
        var sm = new FocusSessionStateMachine();
        var config = new FocusSessionConfig
        {
            Mode = SessionMode.Pomodoro,
            WorkIntervalSeconds = 2,
            ShortBreakSeconds = 1,
            LongBreakSeconds = 3,
            CyclesBeforeLongBreak = 2,
            PlannedSeconds = 2
        };

        sm.Start(config);
        Assert.Equal(PomodoroPhase.Work, sm.CurrentPomodoroPhase);
        Assert.Equal(1, sm.CurrentPomodoroCycle);

        // Simulate work interval completion
        Thread.Sleep(2100);
        var completed = sm.TickAndCheckCompletion(out var session);

        Assert.False(completed);
        Assert.Null(session);
        Assert.Equal(PomodoroPhase.ShortBreak, sm.CurrentPomodoroPhase);
        Assert.Equal(2, sm.CurrentPomodoroCycle);
    }
}
