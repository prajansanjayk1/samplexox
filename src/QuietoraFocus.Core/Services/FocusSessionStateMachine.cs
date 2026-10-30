using System.Diagnostics;
using QuietoraFocus.Core.Models;

namespace QuietoraFocus.Core.Services;

public sealed class SessionStateChangedEventArgs : EventArgs
{
    public SessionState PreviousState { get; }
    public SessionState NewState { get; }
    public SessionLog? CompletedSession { get; }

    public SessionStateChangedEventArgs(SessionState previous, SessionState current, SessionLog? log = null)
    {
        PreviousState = previous;
        NewState = current;
        CompletedSession = log;
    }
}

public sealed class FocusSessionStateMachine
{
    private readonly Stopwatch _activeStopwatch = new();
    private readonly Stopwatch _pauseStopwatch = new();

    private DateTimeOffset _startUtc;
    private int _plannedSeconds;
    private int _accumulatedPauseSeconds;
    private int _distractionCount;

    public SessionState State { get; private set; } = SessionState.Idle;
    public FocusSessionConfig CurrentConfig { get; private set; } = new();

    // Pomodoro tracking
    public PomodoroPhase CurrentPomodoroPhase { get; private set; } = PomodoroPhase.Work;
    public int CurrentPomodoroCycle { get; private set; } = 1;

    public event EventHandler<SessionStateChangedEventArgs>? StateChanged;

    public bool IsActive => State is SessionState.Running or SessionState.Paused;
    public bool IsRunning => State == SessionState.Running;
    public bool IsPaused => State == SessionState.Paused;

    public int ElapsedActiveSeconds => (int)_activeStopwatch.Elapsed.TotalSeconds;
    public int ElapsedPauseSeconds => _accumulatedPauseSeconds + (int)_pauseStopwatch.Elapsed.TotalSeconds;
    public int RemainingSeconds => Math.Max(0, _plannedSeconds - ElapsedActiveSeconds);
    public int DistractionCount => _distractionCount;
    public double ProgressPercentage => _plannedSeconds > 0
        ? Math.Clamp((double)ElapsedActiveSeconds / _plannedSeconds * 100.0, 0, 100)
        : 0;

    public void Start(FocusSessionConfig config)
    {
        if (IsActive)
            throw new InvalidOperationException("A focus session is already active.");

        CurrentConfig = config ?? throw new ArgumentNullException(nameof(config));
        _plannedSeconds = config.PlannedSeconds > 0 ? config.PlannedSeconds : 25 * 60;
        _startUtc = DateTimeOffset.UtcNow;
        _accumulatedPauseSeconds = 0;
        _distractionCount = 0;
        CurrentPomodoroPhase = PomodoroPhase.Work;
        CurrentPomodoroCycle = 1;

        _activeStopwatch.Restart();
        _pauseStopwatch.Reset();

        TransitionTo(SessionState.Running);
    }

    public void Pause()
    {
        if (State != SessionState.Running) return;

        _activeStopwatch.Stop();
        _pauseStopwatch.Restart();

        TransitionTo(SessionState.Paused);
    }

    public void Resume()
    {
        if (State != SessionState.Paused) return;

        _pauseStopwatch.Stop();
        _accumulatedPauseSeconds += (int)_pauseStopwatch.Elapsed.TotalSeconds;
        _pauseStopwatch.Reset();
        _activeStopwatch.Start();

        TransitionTo(SessionState.Running);
    }

    public SessionLog Stop(string endReason = "stopped")
    {
        if (!IsActive && State != SessionState.Idle)
            throw new InvalidOperationException("No session is active to stop.");

        _activeStopwatch.Stop();
        _pauseStopwatch.Stop();

        var actualSeconds = ElapsedActiveSeconds;
        var totalPause = ElapsedPauseSeconds;

        var log = new SessionLog
        {
            Task = CurrentConfig.TaskName,
            Subject = CurrentConfig.Subject,
            ProjectName = CurrentConfig.ProjectName,
            Mode = CurrentConfig.Mode.ToString(),
            StartedUtc = _startUtc,
            EndedUtc = DateTimeOffset.UtcNow,
            PlannedSeconds = _plannedSeconds,
            ActualSeconds = actualSeconds,
            PausedSeconds = totalPause,
            Distractions = _distractionCount,
            EndReason = endReason,
            Notes = CurrentConfig.SessionNotes
        };

        var nextState = endReason switch
        {
            "completed" => SessionState.Completed,
            "emergency" => SessionState.EmergencyStopped,
            _ => SessionState.Stopped
        };

        TransitionTo(nextState, log);
        State = SessionState.Idle; // Ready for subsequent run

        return log;
    }

    public void RecordDistraction()
    {
        if (IsActive)
        {
            _distractionCount++;
        }
    }

    public bool TickAndCheckCompletion(out SessionLog? completedSession)
    {
        completedSession = null;
        if (State != SessionState.Running) return false;

        if (ElapsedActiveSeconds >= _plannedSeconds)
        {
            if (CurrentConfig.Mode == SessionMode.Pomodoro)
            {
                AdvancePomodoroPhase();
                return false;
            }

            completedSession = Stop("completed");
            return true;
        }

        return false;
    }

    private void AdvancePomodoroPhase()
    {
        _activeStopwatch.Restart();

        if (CurrentPomodoroPhase == PomodoroPhase.Work)
        {
            if (CurrentPomodoroCycle >= CurrentConfig.CyclesBeforeLongBreak)
            {
                CurrentPomodoroPhase = PomodoroPhase.LongBreak;
                _plannedSeconds = CurrentConfig.LongBreakSeconds;
                CurrentPomodoroCycle = 1;
            }
            else
            {
                CurrentPomodoroPhase = PomodoroPhase.ShortBreak;
                _plannedSeconds = CurrentConfig.ShortBreakSeconds;
                CurrentPomodoroCycle++;
            }
        }
        else
        {
            CurrentPomodoroPhase = PomodoroPhase.Work;
            _plannedSeconds = CurrentConfig.WorkIntervalSeconds;
        }
    }

    private void TransitionTo(SessionState newState, SessionLog? log = null)
    {
        var prev = State;
        State = newState;
        StateChanged?.Invoke(this, new SessionStateChangedEventArgs(prev, newState, log));
    }
}
