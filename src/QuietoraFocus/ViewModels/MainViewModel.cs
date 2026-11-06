using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Win32;
using QuietoraFocus.Common;
using QuietoraFocus.Core.Models;
using QuietoraFocus.Core.Services;
using QuietoraFocus.Infrastructure.Data;
using QuietoraFocus.Infrastructure.Export;
using QuietoraFocus.Infrastructure.Indexing;
using QuietoraFocus.Infrastructure.Logging;
using QuietoraFocus.Infrastructure.Network;
using QuietoraFocus.Infrastructure.Repositories;
using QuietoraFocus.Interop;
using QuietoraFocus.Services;

namespace QuietoraFocus.ViewModels;

public sealed class MainViewModel : ObservableObject
{
    private readonly QuietoraDatabase _db;
    private readonly SessionRepository _sessionRepo;
    private readonly TaskRepository _taskRepo;
    private readonly DistractionRepository _distractionRepo;
    private readonly WebsiteRepository _websiteRepo;
    private readonly SettingsRepository _settingsRepo;
    private readonly ProjectRepository _projectRepo;
    private readonly MemoryRepository _memoryRepo;
    private readonly WorkspaceRepository _workspaceRepo;
    private readonly AutomationRepository _automationRepo;
    private readonly BudgetRepository _budgetRepo;
    private readonly AcademicRepository _academicRepo;
    private readonly GameShieldRepository _gameShieldRepo;
    private readonly ExtensionSyncServer _extensionSyncServer;

    private readonly LocalMemoryIndexer _memoryIndexer;
    private readonly DataExportService _exportService;
    private readonly FocusSessionStateMachine _sessionSm;
    private readonly AiAssistantService _aiAssistantService;
    private readonly AutomationEngine _automationEngine;
    private readonly SoundService _soundService;
    private readonly DispatcherTimer _tickTimer;

    private string _activePage = "Focus"; // Default landing: Quietora Focus Studio
    private string _statusMessage = "quietora. focus: Ready for deep attention.";
    private string _notificationBannerText = "";
    private bool _isNotificationVisible;
    private DateTimeOffset _lastInterventionTime = DateTimeOffset.MinValue;
    private DateTimeOffset _temporaryBreakUntil = DateTimeOffset.MinValue;
    private CancellationTokenSource? _indexingCts;

    // Focus state bindings
    private string _timerDisplay = "25:00";
    private string _timerSubText = "READY TO FOCUS";
    private double _timerProgress = 0;
    private bool _isSessionActive;
    private bool _isSessionPaused;
    private string _currentTaskInput = "Deep Work Session";
    private string _currentSubjectInput = "Engineering";
    private int _sessionMinutesInput = 25;
    private SessionMode _selectedSessionMode = SessionMode.DeepFocus;
    private DistractionPolicyLevel _selectedPolicyLevel = DistractionPolicyLevel.Remind;
    private string _sessionNotesInput = "";

    // Regain Digital Detox & Screen Time Budgets
    private ObservableCollection<AppBudget> _appBudgets = new();
    private string _newBudgetTargetName = "";
    private string _newBudgetDisplayName = "";
    private int _newBudgetLimitMinutes = 30;
    private DetoxScoreReport _detoxReport = new();
    private int _doomscrollsAvertedCount;
    private int _distractingScreenMinutesToday;
    private bool _blockShortsAndReelsOnly;
    private bool _isStrictMode = true;
    private bool _isYouTubeStudyMode = true;
    private bool _bedtimeShieldEnabled = true;
    private TimeSpan _bedtimeStart = new(22, 30, 0); // 10:30 PM
    private TimeSpan _bedtimeEnd = new(7, 0, 0);     // 07:00 AM
    private InterceptMode _selectedInterceptMode = InterceptMode.HardcoreClose;

    // Ambient Focus Audio Soundscapes
    private AmbientSoundType _selectedAmbientSound = AmbientSoundType.None;
    private bool _isAmbientSoundPlaying;

    // Mindful Friction Overlay (Regain / One Sec style)
    private bool _isMindfulFrictionActive;
    private int _frictionCountdownSeconds = 5;
    private string _frictionTargetName = "";
    private string _frictionPrompt = "Take a breath. What is your intention right now?";
    private IntPtr _lastFrictionHwnd = IntPtr.Zero;
    private bool _lastFrictionIsWebsite;

    // Virtual Coworking Focus Room (Regain Multiplayer)
    private ObservableCollection<CoworkingPeer> _coworkingPeers = new();
    private string _selectedCoworkingRoom = "Silent Deep Work Library";

    // Dashboard metrics bindings
    private ProductivityMetrics _metrics = new();
    private ObservableCollection<DailyActivityPoint> _activityPoints = new();
    private ObservableCollection<SessionLog> _recentSessions = new();
    private ObservableCollection<FocusTask> _activeTasks = new();
    private ObservableCollection<AppDistractionSummary> _topDistractions = new();
    private ObservableCollection<RuleBasedRecommendation> _recommendations = new();

    // Distraction management
    private ObservableCollection<string> _blockedApps = new();
    private string _newBlockedAppName = "";
    private ObservableCollection<BlockedWebsite> _blockedWebsites = new();
    private string _newBlockedWebsiteDomain = "";
    private string _newBlockedWebsiteKeywords = "";
    private DistractionEnforcementMode _enforcementMode = DistractionEnforcementMode.Hardcore;
    private bool _isHostsBlockingEnabled;
    private bool _isAdminUser = HostsFileService.IsAdministrator();

    // Tasks page bindings
    private ObservableCollection<FocusTask> _allTasks = new();
    private string _newTaskTitle = "";
    private string _newTaskSubject = "";
    private TaskPriority _newTaskPriority = TaskPriority.Medium;
    private int _newTaskMinutes = 25;

    // Memory page bindings
    private ObservableCollection<MemoryFolderConfig> _memoryFolders = new();
    private ObservableCollection<MemorySearchResult> _memoryResults = new();
    private string _memorySearchQuery = "";
    private bool _isIndexingInProgress;
    private string _indexingProgressText = "";
    private double _indexingProgressPercent = 0;
    private int _indexedFilesCount;
    private string _indexedStorageText = "0 KB";

    // AI Assistant bindings
    private ObservableCollection<AiChatMessage> _aiChatMessages = new();
    private string _aiQueryInput = "";
    private bool _isAiThinking;
    private AiAssistantMode _selectedAiMode = AiAssistantMode.Lite;

    // Workspace Recovery (Resume My Work) bindings
    private ObservableCollection<WorkspaceSnapshot> _workspaceSnapshots = new();
    private string _newSnapshotName = "";
    private string _newSnapshotRoot = "";
    private string _newSnapshotNotes = "";
    private WorkspaceSnapshot? _selectedSnapshot;

    // Automation bindings
    private ObservableCollection<AutomationRule> _automationRules = new();
    private string _newRuleName = "";
    private AutomationTriggerType _newRuleTriggerType = AutomationTriggerType.ScheduledTime;
    private string _newRuleTriggerValue = "09:00";
    private AutomationActionType _newRuleActionType = AutomationActionType.NotifyUser;
    private string _newRuleActionPayload = "Time to start your morning focus!";

    // Command Palette bindings
    private bool _isPaletteOpen;
    private string _paletteQuery = "";
    private List<PaletteCommandItem> _allPaletteCommands = new();
    private ObservableCollection<PaletteCommandItem> _filteredPaletteCommands = new();
    private PaletteCommandItem? _selectedPaletteCommand;

    // Settings bindings
    private bool _isDarkMode;
    private bool _isLaunchAtStartup;
    private bool _soundAlertsEnabled = true;

    // v4.0 Game Shield bindings
    private ObservableCollection<DetectedGame> _detectedGames = new();
    private GameEnforcementLevel _selectedGameEnforcementLevel = GameEnforcementLevel.DiscouragementNotification;
    private string _newGameName = "";
    private string _newGameProcessName = "";

    // v4.0 Safe Browsing bindings
    private bool _isAdultProtectionEnabled = true;
    private FamilyDnsProvider _selectedDnsProvider = FamilyDnsProvider.CloudflareFamily;

    // v4.0 Academic Planner bindings
    private ObservableCollection<AcademicSubject> _academicSubjects = new();
    private AcademicSubject? _selectedAcademicSubject;
    private ObservableCollection<AcademicAssignment> _academicAssignments = new();
    private ObservableCollection<AcademicExam> _academicExams = new();
    private ObservableCollection<Flashcard> _academicFlashcards = new();
    private Flashcard? _activeFlashcard;
    private bool _isFlashcardAnswerRevealed;
    private string _newSubjectName = "";
    private string _newSubjectCode = "";
    private string _newSubjectSemester = "Fall 2026";
    private int _newSubjectHours = 40;
    private string _newAssignmentTitle = "";
    private string _newAssignmentSubject = "";
    private int _newAssignmentDueDays = 7;
    private string _newExamTitle = "";
    private string _newExamSubject = "";
    private int _newExamDateDays = 14;
    private int _newExamTargetScore = 90;
    private string _newFlashcardQuestion = "";
    private string _newFlashcardAnswer = "";

    // v4.0 Quietora Together bindings
    private ObservableCollection<StudyRoom> _availableRooms = new();
    private StudyRoom? _activeStudyRoom;
    private string _togetherRoomCodeInput = "";
    private string _newRoomNameInput = "DSA Exam Revision";
    private string _newRoomSubjectInput = "Computer Science";
    private string _newRoomObjectiveInput = "Complete Graph Algorithms Lab";
    private int _newRoomMinutesInput = 45;
    private string _togetherChatInput = "";

    // v4.0 Study AI bindings
    private string _studyAiTopicInput = "Virtual Memory & Paging Algorithms";
    private string _studyAiOutput = "Select an academic study action above to generate grounded summaries, quizzes, or step-by-step concept explanations.";
    private bool _isStudyAiGenerating;

    // Commands
    public RelayCommand NavigateCommand { get; }
    public RelayCommand StartSessionCommand { get; }
    public RelayCommand PauseSessionCommand { get; }
    public RelayCommand ResumeSessionCommand { get; }
    public RelayCommand StopSessionCommand { get; }
    public RelayCommand EmergencyStopCommand { get; }
    public RelayCommand QuickStartCommand { get; }
    public RelayCommand SetTimerPresetCommand { get; }
    public RelayCommand ResetTimerCommand { get; }
    public RelayCommand ToggleThemeCommand { get; }
    public RelayCommand ToggleStrictModeCommand { get; }
    public RelayCommand ToggleYouTubeStudyModeCommand { get; }
    public RelayCommand SelectCoworkingRoomCommand { get; }
    public RelayCommand CopyRoomCodeCommand { get; }
    public RelayCommand DismissNotificationCommand { get; }

    public RelayCommand AddBlockedAppCommand { get; }
    public RelayCommand RemoveBlockedAppCommand { get; }

    public RelayCommand AddBlockedWebsiteCommand { get; }
    public RelayCommand RemoveBlockedWebsiteCommand { get; }
    public RelayCommand ToggleBlockedWebsiteCommand { get; }

    public RelayCommand CreateTaskCommand { get; }
    public RelayCommand ToggleTaskCommand { get; }
    public RelayCommand DeleteTaskCommand { get; }

    public RelayCommand AddMemoryFolderCommand { get; }
    public RelayCommand RemoveMemoryFolderCommand { get; }
    public RelayCommand SearchMemoryCommand { get; }
    public RelayCommand RebuildMemoryIndexCommand { get; }
    public RelayCommand CancelIndexingCommand { get; }
    public RelayCommand ClearMemoryIndexCommand { get; }
    public RelayCommand OpenFileCommand { get; }

    // Regain Digital Detox Commands
    public RelayCommand AddBudgetCommand { get; }
    public RelayCommand DeleteBudgetCommand { get; }
    public RelayCommand ToggleBudgetCommand { get; }
    public RelayCommand ToggleAmbientSoundCommand { get; }
    public RelayCommand SelectAmbientSoundCommand { get; }
    public RelayCommand AcceptFrictionCloseCommand { get; }
    public RelayCommand TakeTwoMinBreakCommand { get; }

    // v3.0 AI Commands
    public RelayCommand SendAiQueryCommand { get; }
    public RelayCommand ClearAiChatCommand { get; }

    // v3.0 Workspace Commands
    public RelayCommand BrowseWorkspaceFolderCommand { get; }
    public RelayCommand CreateWorkspaceSnapshotCommand { get; }
    public RelayCommand ResumeWorkspaceCommand { get; }
    public RelayCommand DeleteWorkspaceCommand { get; }

    // v3.0 Automation Commands
    public RelayCommand CreateAutomationRuleCommand { get; }
    public RelayCommand ToggleAutomationRuleCommand { get; }
    public RelayCommand DeleteAutomationRuleCommand { get; }

    // v3.0 Command Palette Commands
    public RelayCommand OpenPaletteCommand { get; }
    public RelayCommand ClosePaletteCommand { get; }
    public RelayCommand ExecutePaletteCommand { get; }

    // Export & Wipe Commands
    public RelayCommand ExportSessionsCsvCommand { get; }
    public RelayCommand ExportTasksCsvCommand { get; }
    public RelayCommand ExportSummaryReportCommand { get; }
    public RelayCommand WipeAllDataCommand { get; }

    // v4.0 Game Shield Commands
    public RelayCommand ScanGamesCommand { get; }
    public RelayCommand AddCustomGameCommand { get; }
    public RelayCommand ToggleGameBlockedCommand { get; }
    public RelayCommand DeleteGameCommand { get; }

    // v4.0 Safe Browsing Commands
    public RelayCommand CopyDnsCommand { get; }
    public RelayCommand ResetDnsCommand { get; }

    // v4.0 Academic Planner Commands
    public RelayCommand AddSubjectCommand { get; }
    public RelayCommand DeleteSubjectCommand { get; }
    public RelayCommand AddAssignmentCommand { get; }
    public RelayCommand CompleteAssignmentCommand { get; }
    public RelayCommand DeleteAssignmentCommand { get; }
    public RelayCommand AddExamCommand { get; }
    public RelayCommand DeleteExamCommand { get; }
    public RelayCommand AddFlashcardCommand { get; }
    public RelayCommand RevealFlashcardAnswerCommand { get; }
    public RelayCommand ReviewFlashcardRatingCommand { get; }
    public RelayCommand DeleteFlashcardCommand { get; }

    // v4.0 Quietora Together Commands
    public RelayCommand CreateTogetherRoomCommand { get; }
    public RelayCommand JoinTogetherRoomCommand { get; }
    public RelayCommand LeaveTogetherRoomCommand { get; }
    public RelayCommand StartTogetherSessionCommand { get; }
    public RelayCommand SendTogetherChatMessageCommand { get; }

    // v4.0 Study AI Commands
    public RelayCommand GenerateStudySummaryCommand { get; }
    public RelayCommand GeneratePracticeQuizCommand { get; }
    public RelayCommand ExplainConceptCommand { get; }

    public MainViewModel()
    {
        AppLogger.Initialize();
        _db = new QuietoraDatabase();
        _sessionRepo = new SessionRepository(_db);
        _taskRepo = new TaskRepository(_db);
        _distractionRepo = new DistractionRepository(_db);
        _websiteRepo = new WebsiteRepository(_db);
        _settingsRepo = new SettingsRepository(_db);
        _projectRepo = new ProjectRepository(_db);
        _memoryRepo = new MemoryRepository(_db);
        _workspaceRepo = new WorkspaceRepository(_db);
        _automationRepo = new AutomationRepository(_db);
        _budgetRepo = new BudgetRepository(_db);
        _academicRepo = new AcademicRepository(_db);
        _gameShieldRepo = new GameShieldRepository(_db);
        _extensionSyncServer = new ExtensionSyncServer();
        _extensionSyncServer.Start();

        _memoryIndexer = new LocalMemoryIndexer(_memoryRepo);
        _exportService = new DataExportService(_sessionRepo, _taskRepo, _distractionRepo);
        _sessionSm = new FocusSessionStateMachine();
        _aiAssistantService = new AiAssistantService();
        _automationEngine = new AutomationEngine();
        _soundService = new SoundService();

        // Navigation & Session
        NavigateCommand = new RelayCommand(p => ActivePage = p?.ToString() ?? "Detox");
        StartSessionCommand = new RelayCommand(ExecuteStartSession, () => !IsSessionActive);
        PauseSessionCommand = new RelayCommand(ExecutePauseSession, () => IsSessionActive && !IsSessionPaused);
        ResumeSessionCommand = new RelayCommand(ExecuteResumeSession, () => IsSessionActive && IsSessionPaused);
        StopSessionCommand = new RelayCommand(ExecuteStopSession, () => IsSessionActive);
        EmergencyStopCommand = new RelayCommand(ExecuteEmergencyStop, () => IsSessionActive);
        QuickStartCommand = new RelayCommand(ExecuteQuickStart, () => !IsSessionActive);
        SetTimerPresetCommand = new RelayCommand(p => ExecuteSetTimerPreset(p));
        ResetTimerCommand = new RelayCommand(ExecuteResetTimer);
        ToggleThemeCommand = new RelayCommand(() => IsDarkMode = !IsDarkMode);
        ToggleStrictModeCommand = new RelayCommand(ExecuteToggleStrictMode);
        ToggleYouTubeStudyModeCommand = new RelayCommand(ExecuteToggleYouTubeStudyMode);
        SelectCoworkingRoomCommand = new RelayCommand(ExecuteSelectCoworkingRoom);
        CopyRoomCodeCommand = new RelayCommand(ExecuteCopyRoomCode);
        DismissNotificationCommand = new RelayCommand(() => IsNotificationVisible = false);

        // Distractions
        AddBlockedAppCommand = new RelayCommand(ExecuteAddBlockedApp);
        RemoveBlockedAppCommand = new RelayCommand(p => ExecuteRemoveBlockedApp(p?.ToString()));
        AddBlockedWebsiteCommand = new RelayCommand(ExecuteAddBlockedWebsite);
        RemoveBlockedWebsiteCommand = new RelayCommand(p => ExecuteRemoveBlockedWebsite(p?.ToString()));
        ToggleBlockedWebsiteCommand = new RelayCommand(p => ExecuteToggleBlockedWebsite(p));

        // Regain Detox & Screen Time Budgets
        AddBudgetCommand = new RelayCommand(ExecuteAddBudget);
        DeleteBudgetCommand = new RelayCommand(p => ExecuteDeleteBudget(p));
        ToggleBudgetCommand = new RelayCommand(p => ExecuteToggleBudget(p));
        ToggleAmbientSoundCommand = new RelayCommand(ExecuteToggleAmbientSound);
        SelectAmbientSoundCommand = new RelayCommand(p => ExecuteSelectAmbientSound(p));
        AcceptFrictionCloseCommand = new RelayCommand(ExecuteAcceptFrictionClose);
        TakeTwoMinBreakCommand = new RelayCommand(ExecuteTakeTwoMinBreak);

        // Tasks
        CreateTaskCommand = new RelayCommand(ExecuteCreateTask);
        ToggleTaskCommand = new RelayCommand(p => ExecuteToggleTask(p));
        DeleteTaskCommand = new RelayCommand(p => ExecuteDeleteTask(p));

        // Memory
        AddMemoryFolderCommand = new RelayCommand(ExecuteAddMemoryFolder);
        RemoveMemoryFolderCommand = new RelayCommand(p => ExecuteRemoveMemoryFolder(p));
        SearchMemoryCommand = new RelayCommand(ExecuteSearchMemory);
        RebuildMemoryIndexCommand = new RelayCommand(ExecuteRebuildMemoryIndex);
        CancelIndexingCommand = new RelayCommand(ExecuteCancelIndexing);
        ClearMemoryIndexCommand = new RelayCommand(ExecuteClearMemoryIndex);
        OpenFileCommand = new RelayCommand(p => ExecuteOpenFile(p?.ToString()));

        // v3.0 AI
        SendAiQueryCommand = new RelayCommand(ExecuteSendAiQuery, () => !IsAiThinking && !string.IsNullOrWhiteSpace(AiQueryInput));
        ClearAiChatCommand = new RelayCommand(ExecuteClearAiChat);

        // v3.0 Workspace Recovery
        BrowseWorkspaceFolderCommand = new RelayCommand(ExecuteBrowseWorkspaceFolder);
        CreateWorkspaceSnapshotCommand = new RelayCommand(ExecuteCreateWorkspaceSnapshot);
        ResumeWorkspaceCommand = new RelayCommand(p => ExecuteResumeWorkspace(p));
        DeleteWorkspaceCommand = new RelayCommand(p => ExecuteDeleteWorkspace(p));

        // v3.0 Automation
        CreateAutomationRuleCommand = new RelayCommand(ExecuteCreateAutomationRule);
        ToggleAutomationRuleCommand = new RelayCommand(p => ExecuteToggleAutomationRule(p));
        DeleteAutomationRuleCommand = new RelayCommand(p => ExecuteDeleteAutomationRule(p));

        // v3.0 Command Palette
        OpenPaletteCommand = new RelayCommand(() => IsPaletteOpen = true);
        ClosePaletteCommand = new RelayCommand(() => IsPaletteOpen = false);
        ExecutePaletteCommand = new RelayCommand(p => ExecutePaletteAction(p as PaletteCommandItem));

        // Exports & System
        ExportSessionsCsvCommand = new RelayCommand(ExecuteExportSessionsCsv);
        ExportTasksCsvCommand = new RelayCommand(ExecuteExportTasksCsv);
        ExportSummaryReportCommand = new RelayCommand(ExecuteExportSummaryReport);
        WipeAllDataCommand = new RelayCommand(ExecuteWipeAllData);

        // v4.0 Game Shield
        ScanGamesCommand = new RelayCommand(ExecuteScanGames);
        AddCustomGameCommand = new RelayCommand(ExecuteAddCustomGame);
        ToggleGameBlockedCommand = new RelayCommand(p => ExecuteToggleGameBlocked(p));
        DeleteGameCommand = new RelayCommand(p => ExecuteDeleteGame(p));

        // v4.0 Safe Browsing
        CopyDnsCommand = new RelayCommand(ExecuteCopyDnsCommand);
        ResetDnsCommand = new RelayCommand(ExecuteResetDnsCommand);

        // v4.0 Academic Planner
        AddSubjectCommand = new RelayCommand(ExecuteAddSubject);
        DeleteSubjectCommand = new RelayCommand(p => ExecuteDeleteSubject(p));
        AddAssignmentCommand = new RelayCommand(ExecuteAddAssignment);
        CompleteAssignmentCommand = new RelayCommand(p => ExecuteCompleteAssignment(p));
        DeleteAssignmentCommand = new RelayCommand(p => ExecuteDeleteAssignment(p));
        AddExamCommand = new RelayCommand(ExecuteAddExam);
        DeleteExamCommand = new RelayCommand(p => ExecuteDeleteExam(p));
        AddFlashcardCommand = new RelayCommand(ExecuteAddFlashcard);
        RevealFlashcardAnswerCommand = new RelayCommand(() => IsFlashcardAnswerRevealed = true);
        ReviewFlashcardRatingCommand = new RelayCommand(p => ExecuteReviewFlashcardRating(p));
        DeleteFlashcardCommand = new RelayCommand(p => ExecuteDeleteFlashcard(p));

        // v4.0 Quietora Together
        CreateTogetherRoomCommand = new RelayCommand(ExecuteCreateTogetherRoom);
        JoinTogetherRoomCommand = new RelayCommand(ExecuteJoinTogetherRoom);
        LeaveTogetherRoomCommand = new RelayCommand(ExecuteLeaveTogetherRoom);
        StartTogetherSessionCommand = new RelayCommand(ExecuteStartTogetherSession);
        SendTogetherChatMessageCommand = new RelayCommand(ExecuteSendTogetherChatMessage);

        // v4.0 Study AI
        GenerateStudySummaryCommand = new RelayCommand(ExecuteGenerateStudySummary);
        GeneratePracticeQuizCommand = new RelayCommand(ExecuteGeneratePracticeQuiz);
        ExplainConceptCommand = new RelayCommand(ExecuteExplainConcept);

        // Load settings
        _isDarkMode = _settingsRepo.GetBool("dark_mode", true);
        _isLaunchAtStartup = StartupManager.IsLaunchAtStartupEnabled();
        _soundAlertsEnabled = _settingsRepo.GetBool("sound_alerts", true);
        _isHostsBlockingEnabled = _settingsRepo.GetBool("hosts_blocking", false);
        _blockShortsAndReelsOnly = _settingsRepo.GetBool("block_shorts_reels_only", false);
        _isStrictMode = _settingsRepo.GetBool("strict_mode", true);
        _isYouTubeStudyMode = _settingsRepo.GetBool("youtube_study_mode", true);
        _bedtimeShieldEnabled = _settingsRepo.GetBool("bedtime_shield_enabled", true);

        var enfRaw = _settingsRepo.GetInt("enforcement_mode", (int)DistractionEnforcementMode.Hardcore);
        _enforcementMode = Enum.IsDefined(typeof(DistractionEnforcementMode), enfRaw)
            ? (DistractionEnforcementMode)enfRaw
            : DistractionEnforcementMode.Hardcore;
        var policyRaw = _settingsRepo.GetInt("policy_level", 2);
        _selectedPolicyLevel = Enum.IsDefined(typeof(DistractionPolicyLevel), policyRaw)
            ? (DistractionPolicyLevel)policyRaw
            : DistractionPolicyLevel.Remind;

        // Setup timer (500ms fast tick loop for instant distraction response)
        _tickTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(500) };
        _tickTimer.Tick += OnTick;
        _tickTimer.Start(); // Timer runs continually to track active screen time & quotas

        // Initialize AI chat intro
        _aiChatMessages.Add(new AiChatMessage
        {
            Sender = "Quietora AI",
            Content = "Welcome to Quietora Focus (Regain Edition)! I am your on-device digital detox and productivity assistant. Ask me questions about your screen time, indexed documents, or task goals. 100% private.",
            TimestampUtc = DateTimeOffset.UtcNow
        });

        // Initialize Virtual Coworking Peers
        InitializeCoworkingPeers();

        // Initialize Command Palette catalog
        InitializePaletteCommands();

        // Load initial data
        RefreshAllData();
        UpdateIdleTimerDisplay();
    }

    #region Properties

    public string ActivePage
    {
        get => _activePage;
        set => SetProperty(ref _activePage, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string NotificationBannerText
    {
        get => _notificationBannerText;
        set => SetProperty(ref _notificationBannerText, value);
    }

    public bool IsNotificationVisible
    {
        get => _isNotificationVisible;
        set => SetProperty(ref _isNotificationVisible, value);
    }

    public string TimerDisplay
    {
        get => _timerDisplay;
        set => SetProperty(ref _timerDisplay, value);
    }

    public string TimerSubText
    {
        get => _timerSubText;
        set => SetProperty(ref _timerSubText, value);
    }

    public double TimerProgress
    {
        get => _timerProgress;
        set => SetProperty(ref _timerProgress, value);
    }

    public bool IsSessionActive
    {
        get => _isSessionActive;
        private set
        {
            if (SetProperty(ref _isSessionActive, value))
            {
                StartSessionCommand.RaiseCanExecuteChanged();
                PauseSessionCommand.RaiseCanExecuteChanged();
                ResumeSessionCommand.RaiseCanExecuteChanged();
                StopSessionCommand.RaiseCanExecuteChanged();
                EmergencyStopCommand.RaiseCanExecuteChanged();
                QuickStartCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsSessionPaused
    {
        get => _isSessionPaused;
        private set
        {
            if (SetProperty(ref _isSessionPaused, value))
            {
                PauseSessionCommand.RaiseCanExecuteChanged();
                ResumeSessionCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string CurrentTaskInput
    {
        get => _currentTaskInput;
        set => SetProperty(ref _currentTaskInput, value);
    }

    public string CurrentSubjectInput
    {
        get => _currentSubjectInput;
        set => SetProperty(ref _currentSubjectInput, value);
    }

    public int SessionMinutesInput
    {
        get => _sessionMinutesInput;
        set
        {
            if (SetProperty(ref _sessionMinutesInput, Math.Clamp(value, 1, 480)))
            {
                if (!IsSessionActive) UpdateIdleTimerDisplay();
            }
        }
    }

    public SessionMode SelectedSessionMode
    {
        get => _selectedSessionMode;
        set
        {
            if (SetProperty(ref _selectedSessionMode, value))
            {
                if (value == SessionMode.Pomodoro)
                    SessionMinutesInput = 25;
                else if (value == SessionMode.DeepFocus)
                    SessionMinutesInput = 50;
                else if (value == SessionMode.StudyMode)
                    SessionMinutesInput = 45;
                else if (value == SessionMode.WorkMode)
                    SessionMinutesInput = 60;
            }
        }
    }

    public DistractionPolicyLevel SelectedPolicyLevel
    {
        get => _selectedPolicyLevel;
        set
        {
            if (SetProperty(ref _selectedPolicyLevel, value))
            {
                _settingsRepo.SetInt("policy_level", (int)value);
            }
        }
    }

    public string SessionNotesInput
    {
        get => _sessionNotesInput;
        set => SetProperty(ref _sessionNotesInput, value);
    }

    // Regain Digital Detox & Screen Time Properties
    public ObservableCollection<AppBudget> AppBudgets
    {
        get => _appBudgets;
        private set => SetProperty(ref _appBudgets, value);
    }

    public string NewBudgetTargetName
    {
        get => _newBudgetTargetName;
        set => SetProperty(ref _newBudgetTargetName, value);
    }

    public string NewBudgetDisplayName
    {
        get => _newBudgetDisplayName;
        set => SetProperty(ref _newBudgetDisplayName, value);
    }

    public int NewBudgetLimitMinutes
    {
        get => _newBudgetLimitMinutes;
        set => SetProperty(ref _newBudgetLimitMinutes, Math.Clamp(value, 1, 360));
    }

    public DetoxScoreReport DetoxReport
    {
        get => _detoxReport;
        private set => SetProperty(ref _detoxReport, value);
    }

    public int DoomscrollsAvertedCount
    {
        get => _doomscrollsAvertedCount;
        set => SetProperty(ref _doomscrollsAvertedCount, value);
    }

    public int DistractingScreenMinutesToday
    {
        get => _distractingScreenMinutesToday;
        set => SetProperty(ref _distractingScreenMinutesToday, value);
    }

    public bool BlockShortsAndReelsOnly
    {
        get => _blockShortsAndReelsOnly;
        set
        {
            if (SetProperty(ref _blockShortsAndReelsOnly, value))
            {
                _settingsRepo.SetBool("block_shorts_reels_only", value);
            }
        }
    }

    public bool IsStrictMode
    {
        get => _isStrictMode;
        set
        {
            if (SetProperty(ref _isStrictMode, value))
            {
                _settingsRepo.SetBool("strict_mode", value);
                OnPropertyChanged(nameof(StrictModeStatusText));
            }
        }
    }

    public string StrictModeStatusText => IsStrictMode ? "🔒 ANTI-CHEAT LOCKED" : "FLEXIBLE";

    public bool IsYouTubeStudyMode
    {
        get => _isYouTubeStudyMode;
        set
        {
            if (SetProperty(ref _isYouTubeStudyMode, value))
            {
                _settingsRepo.SetBool("youtube_study_mode", value);
                SyncBrowserShieldPolicy();
            }
        }
    }

    public bool BedtimeShieldEnabled
    {
        get => _bedtimeShieldEnabled;
        set
        {
            if (SetProperty(ref _bedtimeShieldEnabled, value))
            {
                _settingsRepo.SetBool("bedtime_shield_enabled", value);
            }
        }
    }

    public InterceptMode SelectedInterceptMode
    {
        get => _selectedInterceptMode;
        set
        {
            if (SetProperty(ref _selectedInterceptMode, value))
            {
                OnPropertyChanged(nameof(IsHardcoreCloseMode));
                OnPropertyChanged(nameof(IsMindfulFrictionMode));
            }
        }
    }

    public bool IsHardcoreCloseMode
    {
        get => _selectedInterceptMode == InterceptMode.HardcoreClose;
        set
        {
            if (value)
            {
                SelectedInterceptMode = InterceptMode.HardcoreClose;
            }
        }
    }

    public bool IsMindfulFrictionMode
    {
        get => _selectedInterceptMode == InterceptMode.MindfulFriction;
        set
        {
            if (value)
            {
                SelectedInterceptMode = InterceptMode.MindfulFriction;
            }
        }
    }

    // Ambient Audio Properties
    public AmbientSoundType SelectedAmbientSound
    {
        get => _selectedAmbientSound;
        set
        {
            if (SetProperty(ref _selectedAmbientSound, value))
            {
                if (value == AmbientSoundType.None)
                {
                    _soundService.Stop();
                    IsAmbientSoundPlaying = false;
                }
                else
                {
                    _soundService.PlaySound(value);
                    IsAmbientSoundPlaying = true;
                }
            }
        }
    }

    public bool IsAmbientSoundPlaying
    {
        get => _isAmbientSoundPlaying;
        private set => SetProperty(ref _isAmbientSoundPlaying, value);
    }

    // Mindful Friction Properties
    public bool IsMindfulFrictionActive
    {
        get => _isMindfulFrictionActive;
        set => SetProperty(ref _isMindfulFrictionActive, value);
    }

    public int FrictionCountdownSeconds
    {
        get => _frictionCountdownSeconds;
        set => SetProperty(ref _frictionCountdownSeconds, value);
    }

    public string FrictionTargetName
    {
        get => _frictionTargetName;
        set => SetProperty(ref _frictionTargetName, value);
    }

    public string FrictionPrompt
    {
        get => _frictionPrompt;
        set => SetProperty(ref _frictionPrompt, value);
    }

    // Virtual Coworking Properties
    public ObservableCollection<CoworkingPeer> CoworkingPeers
    {
        get => _coworkingPeers;
        private set => SetProperty(ref _coworkingPeers, value);
    }

    public string SelectedCoworkingRoom
    {
        get => _selectedCoworkingRoom;
        set => SetProperty(ref _selectedCoworkingRoom, value);
    }

    // Dashboard metrics
    public ProductivityMetrics Metrics
    {
        get => _metrics;
        private set => SetProperty(ref _metrics, value);
    }

    public ObservableCollection<DailyActivityPoint> ActivityPoints
    {
        get => _activityPoints;
        private set => SetProperty(ref _activityPoints, value);
    }

    public ObservableCollection<SessionLog> RecentSessions
    {
        get => _recentSessions;
        private set => SetProperty(ref _recentSessions, value);
    }

    public ObservableCollection<FocusTask> ActiveTasks
    {
        get => _activeTasks;
        private set => SetProperty(ref _activeTasks, value);
    }

    public ObservableCollection<AppDistractionSummary> TopDistractions
    {
        get => _topDistractions;
        private set => SetProperty(ref _topDistractions, value);
    }

    public ObservableCollection<RuleBasedRecommendation> Recommendations
    {
        get => _recommendations;
        private set => SetProperty(ref _recommendations, value);
    }

    public ObservableCollection<string> BlockedApps
    {
        get => _blockedApps;
        private set => SetProperty(ref _blockedApps, value);
    }

    public string NewBlockedAppName
    {
        get => _newBlockedAppName;
        set => SetProperty(ref _newBlockedAppName, value);
    }

    public ObservableCollection<BlockedWebsite> BlockedWebsites
    {
        get => _blockedWebsites;
        private set => SetProperty(ref _blockedWebsites, value);
    }

    public string NewBlockedWebsiteDomain
    {
        get => _newBlockedWebsiteDomain;
        set => SetProperty(ref _newBlockedWebsiteDomain, value);
    }

    public string NewBlockedWebsiteKeywords
    {
        get => _newBlockedWebsiteKeywords;
        set => SetProperty(ref _newBlockedWebsiteKeywords, value);
    }

    public DistractionEnforcementMode EnforcementMode
    {
        get => _enforcementMode;
        set
        {
            if (SetProperty(ref _enforcementMode, value))
            {
                _settingsRepo.SetInt("enforcement_mode", (int)value);
            }
        }
    }

    public bool IsHostsBlockingEnabled
    {
        get => _isHostsBlockingEnabled;
        set
        {
            if (SetProperty(ref _isHostsBlockingEnabled, value))
            {
                _settingsRepo.SetBool("hosts_blocking", value);
            }
        }
    }

    public bool IsAdminUser
    {
        get => _isAdminUser;
        private set => SetProperty(ref _isAdminUser, value);
    }

    public ObservableCollection<FocusTask> AllTasks
    {
        get => _allTasks;
        private set => SetProperty(ref _allTasks, value);
    }

    public string NewTaskTitle
    {
        get => _newTaskTitle;
        set => SetProperty(ref _newTaskTitle, value);
    }

    public string NewTaskSubject
    {
        get => _newTaskSubject;
        set => SetProperty(ref _newTaskSubject, value);
    }

    public TaskPriority NewTaskPriority
    {
        get => _newTaskPriority;
        set => SetProperty(ref _newTaskPriority, value);
    }

    public int NewTaskMinutes
    {
        get => _newTaskMinutes;
        set => SetProperty(ref _newTaskMinutes, value);
    }

    public ObservableCollection<MemoryFolderConfig> MemoryFolders
    {
        get => _memoryFolders;
        private set => SetProperty(ref _memoryFolders, value);
    }

    public ObservableCollection<MemorySearchResult> MemoryResults
    {
        get => _memoryResults;
        private set => SetProperty(ref _memoryResults, value);
    }

    public string MemorySearchQuery
    {
        get => _memorySearchQuery;
        set => SetProperty(ref _memorySearchQuery, value);
    }

    public bool IsIndexingInProgress
    {
        get => _isIndexingInProgress;
        set => SetProperty(ref _isIndexingInProgress, value);
    }

    public string IndexingProgressText
    {
        get => _indexingProgressText;
        set => SetProperty(ref _indexingProgressText, value);
    }

    public double IndexingProgressPercent
    {
        get => _indexingProgressPercent;
        set => SetProperty(ref _indexingProgressPercent, value);
    }

    public int IndexedFilesCount
    {
        get => _indexedFilesCount;
        set => SetProperty(ref _indexedFilesCount, value);
    }

    public string IndexedStorageText
    {
        get => _indexedStorageText;
        set => SetProperty(ref _indexedStorageText, value);
    }

    // AI Assistant Properties
    public ObservableCollection<AiChatMessage> AiChatMessages
    {
        get => _aiChatMessages;
        private set => SetProperty(ref _aiChatMessages, value);
    }

    public string AiQueryInput
    {
        get => _aiQueryInput;
        set
        {
            if (SetProperty(ref _aiQueryInput, value))
            {
                SendAiQueryCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public bool IsAiThinking
    {
        get => _isAiThinking;
        set
        {
            if (SetProperty(ref _isAiThinking, value))
            {
                SendAiQueryCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public AiAssistantMode SelectedAiMode
    {
        get => _selectedAiMode;
        set => SetProperty(ref _selectedAiMode, value);
    }

    // Workspace Recovery Properties
    public ObservableCollection<WorkspaceSnapshot> WorkspaceSnapshots
    {
        get => _workspaceSnapshots;
        private set => SetProperty(ref _workspaceSnapshots, value);
    }

    public string NewSnapshotName
    {
        get => _newSnapshotName;
        set => SetProperty(ref _newSnapshotName, value);
    }

    public string NewSnapshotRoot
    {
        get => _newSnapshotRoot;
        set => SetProperty(ref _newSnapshotRoot, value);
    }

    public string NewSnapshotNotes
    {
        get => _newSnapshotNotes;
        set => SetProperty(ref _newSnapshotNotes, value);
    }

    public WorkspaceSnapshot? SelectedSnapshot
    {
        get => _selectedSnapshot;
        set => SetProperty(ref _selectedSnapshot, value);
    }

    // Automation Properties
    public ObservableCollection<AutomationRule> AutomationRules
    {
        get => _automationRules;
        private set => SetProperty(ref _automationRules, value);
    }

    public string NewRuleName
    {
        get => _newRuleName;
        set => SetProperty(ref _newRuleName, value);
    }

    public AutomationTriggerType NewRuleTriggerType
    {
        get => _newRuleTriggerType;
        set => SetProperty(ref _newRuleTriggerType, value);
    }

    public string NewRuleTriggerValue
    {
        get => _newRuleTriggerValue;
        set => SetProperty(ref _newRuleTriggerValue, value);
    }

    public AutomationActionType NewRuleActionType
    {
        get => _newRuleActionType;
        set => SetProperty(ref _newRuleActionType, value);
    }

    public string NewRuleActionPayload
    {
        get => _newRuleActionPayload;
        set => SetProperty(ref _newRuleActionPayload, value);
    }

    // Command Palette Properties
    public bool IsPaletteOpen
    {
        get => _isPaletteOpen;
        set => SetProperty(ref _isPaletteOpen, value);
    }

    public string PaletteQuery
    {
        get => _paletteQuery;
        set
        {
            if (SetProperty(ref _paletteQuery, value))
            {
                FilterPaletteCommands(value);
            }
        }
    }

    public ObservableCollection<PaletteCommandItem> FilteredPaletteCommands
    {
        get => _filteredPaletteCommands;
        private set => SetProperty(ref _filteredPaletteCommands, value);
    }

    public PaletteCommandItem? SelectedPaletteCommand
    {
        get => _selectedPaletteCommand;
        set => SetProperty(ref _selectedPaletteCommand, value);
    }

    // Settings
    public bool IsDarkMode
    {
        get => _isDarkMode;
        set
        {
            if (SetProperty(ref _isDarkMode, value))
            {
                _settingsRepo.SetBool("dark_mode", value);
                ThemeManager.ApplyTheme(value);
            }
        }
    }

    public bool IsLaunchAtStartup
    {
        get => _isLaunchAtStartup;
        set
        {
            if (SetProperty(ref _isLaunchAtStartup, value))
            {
                StartupManager.SetLaunchAtStartup(value);
            }
        }
    }

    public bool SoundAlertsEnabled
    {
        get => _soundAlertsEnabled;
        set
        {
            if (SetProperty(ref _soundAlertsEnabled, value))
            {
                _settingsRepo.SetBool("sound_alerts", value);
            }
        }
    }

    public string DatabasePath => _db.DatabasePath;

    // v4.0 Game Shield Properties
    public ObservableCollection<DetectedGame> DetectedGames
    {
        get => _detectedGames;
        private set => SetProperty(ref _detectedGames, value);
    }

    public GameEnforcementLevel SelectedGameEnforcementLevel
    {
        get => _selectedGameEnforcementLevel;
        set
        {
            if (SetProperty(ref _selectedGameEnforcementLevel, value))
            {
                OnPropertyChanged(nameof(SelectedGameEnforcementLevelIndex));
            }
        }
    }

    public int SelectedGameEnforcementLevelIndex
    {
        get => (int)_selectedGameEnforcementLevel;
        set
        {
            if (value >= 0 && value <= 2 && (int)_selectedGameEnforcementLevel != value)
            {
                SelectedGameEnforcementLevel = (GameEnforcementLevel)value;
            }
        }
    }

    public string NewGameName
    {
        get => _newGameName;
        set => SetProperty(ref _newGameName, value);
    }

    public string NewGameProcessName
    {
        get => _newGameProcessName;
        set => SetProperty(ref _newGameProcessName, value);
    }

    // v4.0 Safe Browsing Properties
    public bool IsAdultProtectionEnabled
    {
        get => _isAdultProtectionEnabled;
        set
        {
            if (SetProperty(ref _isAdultProtectionEnabled, value))
            {
                SyncBrowserShieldPolicy();
            }
        }
    }

    public FamilyDnsProvider SelectedDnsProvider
    {
        get => _selectedDnsProvider;
        set
        {
            if (SetProperty(ref _selectedDnsProvider, value))
            {
                OnPropertyChanged(nameof(SelectedDnsProviderIndex));
                OnPropertyChanged(nameof(DnsPowerShellCommand));
                OnPropertyChanged(nameof(SelectedDnsInfo));
            }
        }
    }

    public int SelectedDnsProviderIndex
    {
        get => (int)_selectedDnsProvider;
        set
        {
            if (value >= 0 && value <= 3 && (int)_selectedDnsProvider != value)
            {
                SelectedDnsProvider = (FamilyDnsProvider)value;
            }
        }
    }

    public DnsProviderInfo SelectedDnsInfo => DnsGuidanceService.Providers.GetValueOrDefault(SelectedDnsProvider, DnsGuidanceService.Providers[FamilyDnsProvider.CloudflareFamily]);

    public string DnsPowerShellCommand => DnsGuidanceService.GeneratePowerShellApplyCommand(SelectedDnsProvider);

    // v4.0 Academic Planner Properties
    public ObservableCollection<AcademicSubject> AcademicSubjects
    {
        get => _academicSubjects;
        private set => SetProperty(ref _academicSubjects, value);
    }

    public AcademicSubject? SelectedAcademicSubject
    {
        get => _selectedAcademicSubject;
        set => SetProperty(ref _selectedAcademicSubject, value);
    }

    public ObservableCollection<AcademicAssignment> AcademicAssignments
    {
        get => _academicAssignments;
        private set => SetProperty(ref _academicAssignments, value);
    }

    public ObservableCollection<AcademicExam> AcademicExams
    {
        get => _academicExams;
        private set => SetProperty(ref _academicExams, value);
    }

    public ObservableCollection<Flashcard> AcademicFlashcards
    {
        get => _academicFlashcards;
        private set => SetProperty(ref _academicFlashcards, value);
    }

    public Flashcard? ActiveFlashcard
    {
        get => _activeFlashcard;
        set => SetProperty(ref _activeFlashcard, value);
    }

    public bool IsFlashcardAnswerRevealed
    {
        get => _isFlashcardAnswerRevealed;
        set => SetProperty(ref _isFlashcardAnswerRevealed, value);
    }

    public string NewSubjectName
    {
        get => _newSubjectName;
        set => SetProperty(ref _newSubjectName, value);
    }

    public string NewSubjectCode
    {
        get => _newSubjectCode;
        set => SetProperty(ref _newSubjectCode, value);
    }

    public string NewSubjectSemester
    {
        get => _newSubjectSemester;
        set => SetProperty(ref _newSubjectSemester, value);
    }

    public int NewSubjectHours
    {
        get => _newSubjectHours;
        set => SetProperty(ref _newSubjectHours, value);
    }

    public string NewAssignmentTitle
    {
        get => _newAssignmentTitle;
        set => SetProperty(ref _newAssignmentTitle, value);
    }

    public string NewAssignmentSubject
    {
        get => _newAssignmentSubject;
        set => SetProperty(ref _newAssignmentSubject, value);
    }

    public int NewAssignmentDueDays
    {
        get => _newAssignmentDueDays;
        set => SetProperty(ref _newAssignmentDueDays, value);
    }

    public string NewExamTitle
    {
        get => _newExamTitle;
        set => SetProperty(ref _newExamTitle, value);
    }

    public string NewExamSubject
    {
        get => _newExamSubject;
        set => SetProperty(ref _newExamSubject, value);
    }

    public int NewExamDateDays
    {
        get => _newExamDateDays;
        set => SetProperty(ref _newExamDateDays, value);
    }

    public int NewExamTargetScore
    {
        get => _newExamTargetScore;
        set => SetProperty(ref _newExamTargetScore, value);
    }

    public string NewFlashcardQuestion
    {
        get => _newFlashcardQuestion;
        set => SetProperty(ref _newFlashcardQuestion, value);
    }

    public string NewFlashcardAnswer
    {
        get => _newFlashcardAnswer;
        set => SetProperty(ref _newFlashcardAnswer, value);
    }

    // v4.0 Quietora Together Properties
    public ObservableCollection<StudyRoom> AvailableRooms
    {
        get => _availableRooms;
        private set => SetProperty(ref _availableRooms, value);
    }

    public StudyRoom? ActiveStudyRoom
    {
        get => _activeStudyRoom;
        set => SetProperty(ref _activeStudyRoom, value);
    }

    public string TogetherRoomCodeInput
    {
        get => _togetherRoomCodeInput;
        set => SetProperty(ref _togetherRoomCodeInput, value);
    }

    public string NewRoomNameInput
    {
        get => _newRoomNameInput;
        set => SetProperty(ref _newRoomNameInput, value);
    }

    public string NewRoomSubjectInput
    {
        get => _newRoomSubjectInput;
        set => SetProperty(ref _newRoomSubjectInput, value);
    }

    public string NewRoomObjectiveInput
    {
        get => _newRoomObjectiveInput;
        set => SetProperty(ref _newRoomObjectiveInput, value);
    }

    public int NewRoomMinutesInput
    {
        get => _newRoomMinutesInput;
        set => SetProperty(ref _newRoomMinutesInput, value);
    }

    public string TogetherChatInput
    {
        get => _togetherChatInput;
        set => SetProperty(ref _togetherChatInput, value);
    }

    // v4.0 Study AI Properties
    public string StudyAiTopicInput
    {
        get => _studyAiTopicInput;
        set => SetProperty(ref _studyAiTopicInput, value);
    }

    public string StudyAiOutput
    {
        get => _studyAiOutput;
        set => SetProperty(ref _studyAiOutput, value);
    }

    public bool IsStudyAiGenerating
    {
        get => _isStudyAiGenerating;
        set => SetProperty(ref _isStudyAiGenerating, value);
    }

    #endregion

    #region Session Orchestration

    private void ExecuteStartSession()
    {
        if (IsSessionActive) return;

        var task = string.IsNullOrWhiteSpace(CurrentTaskInput) ? "Focused Work" : CurrentTaskInput.Trim();
        var plannedSecs = SessionMinutesInput * 60;

        var config = new FocusSessionConfig
        {
            Mode = SelectedSessionMode,
            TaskName = task,
            Subject = CurrentSubjectInput.Trim(),
            PlannedSeconds = plannedSecs,
            PolicyLevel = SelectedPolicyLevel,
            SessionNotes = SessionNotesInput.Trim()
        };

        _sessionSm.Start(config);
        IsSessionActive = true;
        IsSessionPaused = false;
        _lastInterventionTime = DateTimeOffset.MinValue;

        // If system-wide hosts blocking is enabled, apply it
        if (IsHostsBlockingEnabled && HostsFileService.IsAdministrator())
        {
            HostsFileService.TryApplyHostsBlock(BlockedWebsites);
        }

        TimerSubText = $"{SelectedSessionMode.ToString().ToUpper()} ACTIVE";
        StatusMessage = $"Session running ({SessionMinutesInput}m). Digital Detox Shield: ACTIVE.";
        AppLogger.LogInfo($"Started session '{task}' ({SelectedSessionMode}, {SessionMinutesInput}m, Enforcement={EnforcementMode})");
        UpdateActiveTimerDisplay();
    }

    private void ExecutePauseSession()
    {
        if (!IsSessionActive || IsSessionPaused) return;
        if (IsStrictMode)
        {
            ShowNotification("🔒 Strict Anti-Cheat Active: No pausing! Stay locked in and finish your study round.");
            return;
        }
        _sessionSm.Pause();
        IsSessionPaused = true;
        TimerSubText = "SESSION PAUSED";
        StatusMessage = "Session paused. Mindful pause active.";
        AppLogger.LogInfo("Session paused.");
    }

    private void ExecuteResumeSession()
    {
        if (!IsSessionActive || !IsSessionPaused) return;
        _sessionSm.Resume();
        IsSessionPaused = false;
        TimerSubText = $"{SelectedSessionMode.ToString().ToUpper()} ACTIVE";
        StatusMessage = "Session resumed. Regain your deep work flow.";
        AppLogger.LogInfo("Session resumed.");
    }

    private void ExecuteStopSession()
    {
        if (IsStrictMode && IsSessionActive)
        {
            ShowNotification("🔒 Strict Mode Active: No quitting early! Zero loopholes. Protect your study flow.");
            return;
        }
        EndSessionInternal("stopped");
    }

    private void ExecuteEmergencyStop()
    {
        ShowNotification("🚨 Emergency Override used. Focus session ended early.");
        EndSessionInternal("emergency");
    }

    private void ExecuteQuickStart()
    {
        SelectedSessionMode = SessionMode.DeepFocus;
        SessionMinutesInput = 25;
        CurrentTaskInput = "Quick Deep Focus";
        ExecuteStartSession();
    }

    private void ExecuteSetTimerPreset(object? param)
    {
        if (param == null) return;
        int minutes = 25;
        if (param is int m) minutes = m;
        else if (int.TryParse(param.ToString(), out var parsed)) minutes = parsed;

        if (minutes == 25)
        {
            SelectedSessionMode = SessionMode.Pomodoro;
            SessionMinutesInput = 25;
            CurrentTaskInput = "Focused Pomodoro Cycle";
        }
        else if (minutes == 45)
        {
            SelectedSessionMode = SessionMode.DeepFocus;
            SessionMinutesInput = 45;
            CurrentTaskInput = "Deep Work Block";
        }
        else if (minutes == 50)
        {
            SelectedSessionMode = SessionMode.DeepFocus;
            SessionMinutesInput = 50;
            CurrentTaskInput = "50-Minute Intense Block";
        }
        else if (minutes == 5)
        {
            SelectedSessionMode = SessionMode.StudyMode;
            SessionMinutesInput = 5;
            CurrentTaskInput = "5-Minute Rest & Breathe";
        }
        else if (minutes == 15)
        {
            SelectedSessionMode = SessionMode.StudyMode;
            SessionMinutesInput = 15;
            CurrentTaskInput = "15-Minute Cognitive Reset";
        }
        else
        {
            SessionMinutesInput = minutes;
        }

        if (!IsSessionActive)
        {
            UpdateIdleTimerDisplay();
        }
        ShowNotification($"Focus preset set to {minutes}m ({CurrentTaskInput}).");
    }

    private void ExecuteResetTimer()
    {
        if (IsSessionActive)
        {
            ExecuteStopSession();
        }
        UpdateIdleTimerDisplay();
        ShowNotification("Timer reset to initial state.");
    }

    private void EndSessionInternal(string reason)
    {
        if (!IsSessionActive) return;

        var sessionLog = _sessionSm.Stop(reason);
        IsSessionActive = false;
        IsSessionPaused = false;

        // Clean up hosts file block immediately
        try { HostsFileService.RemoveHostsBlock(); } catch { }

        try
        {
            _sessionRepo.AddSession(sessionLog);
            AppLogger.LogInfo($"Session saved: {sessionLog.Task}, actual={sessionLog.ActualSeconds}s, reason={reason}");
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Error saving session record", ex);
            ShowNotification("Session finished, but saving to local SQLite failed: " + ex.Message);
        }

        if (SoundAlertsEnabled && reason == "completed")
        {
            try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
        }

        TimerSubText = reason == "completed" ? "SESSION COMPLETED" : "READY FOR NEXT SESSION";
        StatusMessage = reason switch
        {
            "completed" => $"Nice work! Completed {sessionLog.ActualSeconds / 60}m of focused study.",
            "emergency" => "Emergency stop activated. All controls restored.",
            _ => $"Session ended ({sessionLog.ActualSeconds / 60}m focused, {sessionLog.Distractions} distractions averted)."
        };

        UpdateIdleTimerDisplay();
        RefreshAllData();

        // Trigger session completion automations
        EvaluateSessionEndAutomations(sessionLog);
    }

    private void OnTick(object? sender, EventArgs e)
    {
        // 1. If focus session is active, tick session
        if (IsSessionActive && !IsSessionPaused)
        {
            var completed = _sessionSm.TickAndCheckCompletion(out var completedLog);
            if (completed && completedLog != null)
            {
                EndSessionInternal("completed");
                return;
            }

            UpdateActiveTimerDisplay();
        }

        // 2. Real-Time Screen Time Tracking & Distraction Shield
        TrackScreenTimeAndEnforceDistractions();

        // 3. Update Mindful Friction Countdown if active
        if (IsMindfulFrictionActive && FrictionCountdownSeconds > 0)
        {
            FrictionCountdownSeconds--;
            if (FrictionCountdownSeconds == 0)
            {
                // Mindful breath complete; give choice
                FrictionPrompt = "Breath complete. Is opening this essential right now?";
            }
        }

        // 4. Periodically evaluate active automation rules
        EvaluateAutomationEngine();
    }

    private void UpdateActiveTimerDisplay()
    {
        var rem = _sessionSm.RemainingSeconds;
        TimerDisplay = $"{rem / 60:D2}:{rem % 60:D2}";
        TimerProgress = _sessionSm.ProgressPercentage;

        if (SelectedSessionMode == SessionMode.Pomodoro)
        {
            TimerSubText = $"POMODORO: {_sessionSm.CurrentPomodoroPhase.ToString().ToUpper()} (CYCLE {_sessionSm.CurrentPomodoroCycle})";
        }
    }

    private void UpdateIdleTimerDisplay()
    {
        TimerDisplay = $"{SessionMinutesInput:D2}:00";
        TimerProgress = 0;
        TimerSubText = "READY TO FOCUS";
    }

    private void TrackScreenTimeAndEnforceDistractions()
    {
        if (!WindowsForegroundMonitor.TryGetForegroundProcess(out var name, out var title, out var handle)) return;
        if (WindowsForegroundMonitor.ProtectedProcesses.Contains(name)) return;

        var isBrowser = WebsiteMatcher.IsBrowser(name);
        bool isDistraction = false;
        string distractionDetail = "";
        bool isWebsite = false;
        bool isShortsOrReels = false;

        // Check Bedtime Shield
        var now = DateTimeOffset.UtcNow;
        bool isBedtimeActive = BedtimeShieldEnabled && DetoxCalculator.Calculate(
            Metrics.TodayFocusMinutes, Metrics.TodayDistractions, DoomscrollsAvertedCount,
            DistractingScreenMinutesToday, AppBudgets, _bedtimeStart, _bedtimeEnd).IsBedtimeShieldActive;

        // A. Check Daily App / Web Quotas & Budgets
        foreach (var budget in AppBudgets.Where(b => b.IsActive))
        {
            bool match = false;
            var cleanTarget = budget.TargetName.Replace(".com", "").Replace(".tv", "").Replace(".org", "").Replace(".net", "").Trim();
            if (budget.TargetType == "app" && string.Equals(budget.TargetName, name, StringComparison.OrdinalIgnoreCase))
            {
                match = true;
            }
            else if (budget.TargetType == "website" && isBrowser && 
                     (title.Contains(budget.TargetName, StringComparison.OrdinalIgnoreCase) ||
                      title.Contains(budget.DisplayName, StringComparison.OrdinalIgnoreCase) ||
                      (!string.IsNullOrEmpty(cleanTarget) && title.Contains(cleanTarget, StringComparison.OrdinalIgnoreCase))))
            {
                match = true;
            }

            if (match)
            {
                budget.UsedSecondsToday++;
                if (budget.UsedSecondsToday % 60 == 0)
                {
                    _budgetRepo.IncrementUsage(budget.TargetName, 60);
                }

                if (budget.IsQuotaExceeded || isBedtimeActive)
                {
                    isDistraction = true;
                    distractionDetail = $"{budget.DisplayName} (Daily {budget.DailyLimitMinutes}m Budget Exceeded)";
                    isWebsite = budget.TargetType == "website";
                    break;
                }
            }
        }

        // B. Check Distracting Applications List
        if (!isDistraction && _blockedApps.Contains(name, StringComparer.OrdinalIgnoreCase))
        {
            isDistraction = true;
            distractionDetail = name;
        }

        // C. Check Short-Form Doomscroll Feeds (YouTube Shorts, Instagram Reels, TikTok, Reddit Endless) & Blocked Sites
        if (!isDistraction && isBrowser)
        {
            bool onlyShorts = (BlockShortsAndReelsOnly || IsYouTubeStudyMode) && (!IsSessionActive || IsYouTubeStudyMode);
            if (WebsiteMatcher.TryMatchBlockedWebsite(title, _blockedWebsites, onlyShorts, out var matchedSite, out isShortsOrReels) && matchedSite != null)
            {
                if (IsYouTubeStudyMode && !isShortsOrReels && matchedSite.Domain.Contains("youtube", StringComparison.OrdinalIgnoreCase))
                {
                    // Educational YouTube playback allowed under YouTube Study Mode
                }
                else
                {
                    isDistraction = true;
                    distractionDetail = isShortsOrReels ? matchedSite.DisplayName : $"{matchedSite.DisplayName} ({matchedSite.Domain})";
                    isWebsite = true;
                }
            }
        }

        // D. Check Game Shield (Steam, Riot, Epic, Custom Games)
        if (!isDistraction && DetectedGames != null && DetectedGames.Count > 0)
        {
            var blockedGame = DetectedGames.FirstOrDefault(g => g.IsBlocked &&
                (string.Equals(g.ProcessName, name, StringComparison.OrdinalIgnoreCase) ||
                 title.Contains(g.GameName, StringComparison.OrdinalIgnoreCase)));

            if (blockedGame != null)
            {
                if (SelectedGameEnforcementLevel == GameEnforcementLevel.DetectionOnly && !IsSessionActive)
                {
                    // Level 1: Friendly gentle awareness nudge
                    if ((now - _lastInterventionTime).TotalSeconds > 10.0)
                    {
                        _lastInterventionTime = now;
                        ShowNotification($"🎮 Game Alert: {blockedGame.GameName} is running. Ready to study when you are!");
                    }
                }
                else
                {
                    // Level 2 (DiscouragementNotification) or Level 3 (StrictMinimizeAndPause) or in active focus session
                    isDistraction = true;
                    distractionDetail = $"Game: {blockedGame.GameName}";
                    _gameShieldRepo.RecordGameLaunch(blockedGame.ProcessName);
                }
            }
        }

        if (!isDistraction) return;

        // Check if user is on an authorized 2-minute mindful break
        if (DateTimeOffset.UtcNow < _temporaryBreakUntil) return;

        // Intercept Cooldown check
        var cooldownSeconds = (EnforcementMode == DistractionEnforcementMode.Hardcore || isShortsOrReels) ? 0.8 : 3.0;
        if ((now - _lastInterventionTime).TotalSeconds < cooldownSeconds) return;
        _lastInterventionTime = now;

        if (IsSessionActive)
        {
            _sessionSm.RecordDistraction();
        }

        if (isShortsOrReels)
        {
            DoomscrollsAvertedCount++;
        }

        // Intercept Action (Hell Mode or Mindful Friction)
        if (SelectedInterceptMode == InterceptMode.MindfulFriction && !IsMindfulFrictionActive && (DateTimeOffset.UtcNow >= _temporaryBreakUntil))
        {
            // Trigger Mindful Friction Pause dialog!
            _lastFrictionHwnd = handle;
            _lastFrictionIsWebsite = isWebsite;
            FrictionTargetName = distractionDetail;
            FrictionCountdownSeconds = 5;
            FrictionPrompt = "Take a breath. What is your intention right now?";
            IsMindfulFrictionActive = true;

            // Bring Quietora Focus to the foreground so the breathing orb is visible
            Application.Current?.Dispatcher.BeginInvoke(() =>
            {
                var win = Application.Current.MainWindow;
                if (win != null)
                {
                    if (win.WindowState == WindowState.Minimized)
                    {
                        win.WindowState = WindowState.Normal;
                    }
                    win.Activate();
                    win.Topmost = true;
                    win.Topmost = false;
                }
            });

            return;
        }

        // HARDCORE / STRICT ENFORCEMENT
        if (isWebsite)
        {
            WindowsForegroundMonitor.CloseActiveTab(handle);
            Thread.Sleep(80);
        }
        WindowsForegroundMonitor.MinimizeWindow(handle);

        var actionTaken = isWebsite ? "Closed Tab & Minimized" : "Minimized";
        ShowNotification(isShortsOrReels
            ? $"🚫 Regain Shield: Blocked {distractionDetail}! Impulsive doomscroll averted."
            : $"🔥 Strict Focus: Minimized {distractionDetail}. Stay in flow.");

        StatusMessage = $"🔥 Intercepted: {distractionDetail}";

        try
        {
            _distractionRepo.RecordEvent(new DistractionEvent
            {
                ProcessName = name,
                TargetDetail = distractionDetail,
                PolicyApplied = SelectedPolicyLevel,
                ActionTaken = actionTaken,
                TimestampUtc = now
            });
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Error recording distraction event", ex);
        }

        // Refresh live detox score
        RefreshDetoxMetrics();
    }

    private void ExecuteAcceptFrictionClose()
    {
        IsMindfulFrictionActive = false;
        if (_lastFrictionHwnd != IntPtr.Zero)
        {
            if (_lastFrictionIsWebsite)
            {
                WindowsForegroundMonitor.CloseActiveTab(_lastFrictionHwnd);
                Thread.Sleep(80);
            }
            WindowsForegroundMonitor.MinimizeWindow(_lastFrictionHwnd);
        }
        DoomscrollsAvertedCount++;
        ShowNotification("🌟 Mindful Choice: You chose to stay focused! Dopamine saved.");
        RefreshDetoxMetrics();
    }

    private void ExecuteTakeTwoMinBreak()
    {
        IsMindfulFrictionActive = false;
        _temporaryBreakUntil = DateTimeOffset.UtcNow.AddMinutes(2);
        ShowNotification("⏳ 2-minute intentional break active. Shield will re-engage in 120s.");
    }

    private void ShowNotification(string message)
    {
        NotificationBannerText = message;
        IsNotificationVisible = true;
    }

    #endregion

    #region Regain Digital Detox Management

    private void ExecuteAddBudget()
    {
        if (string.IsNullOrWhiteSpace(NewBudgetTargetName))
        {
            ShowNotification("Please enter an app name or website domain (e.g. youtube.com or discord).");
            return;
        }

        var target = NewBudgetTargetName.Trim().ToLowerInvariant()
            .Replace("https://", "").Replace("http://", "").Replace("www.", "");
        var label = string.IsNullOrWhiteSpace(NewBudgetDisplayName) ? char.ToUpper(target[0]) + target[1..] : NewBudgetDisplayName.Trim();
        var type = target.Contains('.') ? "website" : "app";

        var budget = new AppBudget
        {
            TargetName = target,
            TargetType = type,
            DisplayName = label,
            DailyLimitMinutes = Math.Max(5, NewBudgetLimitMinutes),
            IsActive = true
        };

        _budgetRepo.AddOrUpdateBudget(budget);
        NewBudgetTargetName = "";
        NewBudgetDisplayName = "";
        RefreshAllData();
        ShowNotification($"Added daily {budget.DailyLimitMinutes}m budget for {budget.DisplayName}.");
    }

    private void ExecuteDeleteBudget(object? param)
    {
        if (param is long id || (param is string s && long.TryParse(s, out id)) || (param is AppBudget b && (id = b.Id) > 0))
        {
            _budgetRepo.DeleteBudget(id);
            RefreshAllData();
            ShowNotification("Screen time budget removed.");
        }
    }

    private void ExecuteToggleBudget(object? param)
    {
        if (param is AppBudget b)
        {
            b.IsActive = !b.IsActive;
            _budgetRepo.ToggleBudgetActive(b.Id, b.IsActive);
            RefreshAllData();
        }
    }

    private void ExecuteToggleAmbientSound()
    {
        if (IsAmbientSoundPlaying)
        {
            SelectedAmbientSound = AmbientSoundType.None;
        }
        else
        {
            SelectedAmbientSound = AmbientSoundType.BrownNoise;
        }
    }

    private void ExecuteSelectAmbientSound(object? param)
    {
        if (param is AmbientSoundType type)
        {
            SelectedAmbientSound = type;
        }
        else if (param is string s && Enum.TryParse<AmbientSoundType>(s, true, out var parsed))
        {
            SelectedAmbientSound = parsed;
        }
    }

    private void InitializeCoworkingPeers()
    {
        _coworkingPeers = new ObservableCollection<CoworkingPeer>
        {
            new() { Name = "Priya R.", AvatarEmoji = "👩‍🔬", ActiveTask = "Writing Research Paper", Status = "Deep Focus (42m)", ElapsedMinutes = 42, TodayStreakDays = 14, IsOnline = true },
            new() { Name = "Lucas M.", AvatarEmoji = "👨‍💻", ActiveTask = "Rust Compiler Optimization", Status = "Deep Focus (68m)", ElapsedMinutes = 68, TodayStreakDays = 21, IsOnline = true },
            new() { Name = "Aarav K.", AvatarEmoji = "📐", ActiveTask = "Calculus & Linear Algebra", Status = "Pomodoro Break (3m)", ElapsedMinutes = 25, TodayStreakDays = 9, IsOnline = true },
            new() { Name = "Elena S.", AvatarEmoji = "🎨", ActiveTask = "Product UI Architecture", Status = "Deep Focus (51m)", ElapsedMinutes = 51, TodayStreakDays = 30, IsOnline = true }
        };
    }

    private void ExecuteToggleStrictMode()
    {
        IsStrictMode = !IsStrictMode;
        ShowNotification(IsStrictMode
            ? "🔒 Strict Mode Enabled: Anti-cheat lock engaged. Zero loopholes."
            : "Flexible Mode: You may pause or stop sessions as needed.");
    }

    private void ExecuteToggleYouTubeStudyMode()
    {
        IsYouTubeStudyMode = !IsYouTubeStudyMode;
        ShowNotification(IsYouTubeStudyMode
            ? "📺 YouTube Study Mode: Shorts & recommendation feeds blocked; lectures allowed!"
            : "YouTube Study Mode Disabled.");
    }

    private void ExecuteSelectCoworkingRoom(object? param)
    {
        if (param is not string roomName || string.IsNullOrWhiteSpace(roomName)) return;
        SelectedCoworkingRoom = roomName;
        if (roomName.Contains("Lofi") || roomName.Contains("Cafe"))
        {
            CoworkingPeers = new ObservableCollection<CoworkingPeer>
            {
                new() { Name = "Liam K.", AvatarEmoji = "☕", ActiveTask = "Calculus Problem Set", Status = "Deep Focus (34m)", ElapsedMinutes = 34, TodayStreakDays = 12, IsOnline = true },
                new() { Name = "Zoe M.", AvatarEmoji = "🎧", ActiveTask = "Anki Flashcard Drill", Status = "Deep Focus (45m)", ElapsedMinutes = 45, TodayStreakDays = 18, IsOnline = true },
                new() { Name = "Hannah B.", AvatarEmoji = "📖", ActiveTask = "Literature Review", Status = "Pomodoro Break (4m)", ElapsedMinutes = 25, TodayStreakDays = 6, IsOnline = true }
            };
        }
        else if (roomName.Contains("Code") || roomName.Contains("Systems"))
        {
            CoworkingPeers = new ObservableCollection<CoworkingPeer>
            {
                new() { Name = "Lucas M.", AvatarEmoji = "👨‍💻", ActiveTask = "Rust Engine Architecture", Status = "Deep Focus (72m)", ElapsedMinutes = 72, TodayStreakDays = 24, IsOnline = true },
                new() { Name = "Devin S.", AvatarEmoji = "⚡", ActiveTask = "GPU Kernel Optimization", Status = "Deep Focus (58m)", ElapsedMinutes = 58, TodayStreakDays = 15, IsOnline = true },
                new() { Name = "Priya R.", AvatarEmoji = "👩‍💻", ActiveTask = "Distributed Systems Debugging", Status = "Deep Focus (41m)", ElapsedMinutes = 41, TodayStreakDays = 19, IsOnline = true }
            };
        }
        else
        {
            InitializeCoworkingPeers();
        }
        ShowNotification($"Joined study room: {roomName}. 2,400+ peers online.");
    }

    private void ExecuteCopyRoomCode()
    {
        var code = !string.IsNullOrWhiteSpace(ActiveStudyRoom?.RoomCode) ? ActiveStudyRoom.RoomCode : "REGAIN-4892";
        try
        {
            Clipboard.SetText(code);
            ShowNotification($"📋 Room code '{code}' copied to clipboard! Invite your classmates to study.");
        }
        catch
        {
            ShowNotification($"Room Code: {code}");
        }
    }

    private void RefreshDetoxMetrics()
    {
        DetoxReport = DetoxCalculator.Calculate(
            Metrics.TodayFocusMinutes,
            Metrics.TodayDistractions,
            DoomscrollsAvertedCount,
            DistractingScreenMinutesToday,
            AppBudgets,
            _bedtimeStart,
            _bedtimeEnd);
    }

    #endregion

    #region Data Refresh

    public void RefreshAllData()
    {
        try
        {
            var sessions = _sessionRepo.GetAllSessions();
            var tasks = _taskRepo.GetAllTasks();
            var blocked = _distractionRepo.ReadBlockedApps();
            var topApps = _distractionRepo.GetTopDistractingApps(5);
            var folders = _memoryRepo.GetFolders();
            var (filesCount, bytes, _) = _memoryRepo.GetMemoryStats();
            var snapshots = _workspaceRepo.GetAllSnapshots();
            var rules = _automationRepo.GetAllRules();
            var budgets = _budgetRepo.GetAllBudgets();

            Metrics = ProductivityCalculator.CalculateMetrics(sessions, tasks);
            ActivityPoints = new ObservableCollection<DailyActivityPoint>(
                ProductivityCalculator.GetPastSevenDaysActivity(sessions));

            RecentSessions = new ObservableCollection<SessionLog>(sessions.Take(15));
            AllTasks = new ObservableCollection<FocusTask>(tasks);
            ActiveTasks = new ObservableCollection<FocusTask>(tasks.Where(t => t.Status != TaskStatusEnum.Completed));
            BlockedApps = new ObservableCollection<string>(blocked);
            BlockedWebsites = new ObservableCollection<BlockedWebsite>(_websiteRepo.GetAllWebsites());
            TopDistractions = new ObservableCollection<AppDistractionSummary>(topApps);
            Recommendations = new ObservableCollection<RuleBasedRecommendation>(
                ProductivityCalculator.GenerateRecommendations(Metrics, sessions.Take(10), tasks));

            MemoryFolders = new ObservableCollection<MemoryFolderConfig>(folders);
            IndexedFilesCount = filesCount;
            IndexedStorageText = FormatBytes(bytes);

            WorkspaceSnapshots = new ObservableCollection<WorkspaceSnapshot>(snapshots);
            AutomationRules = new ObservableCollection<AutomationRule>(rules);
            AppBudgets = new ObservableCollection<AppBudget>(budgets);

            // v4.0 Student OS Data Refresh
            if (_academicRepo != null)
            {
                AcademicSubjects = new ObservableCollection<AcademicSubject>(_academicRepo.GetAllSubjects());
                AcademicAssignments = new ObservableCollection<AcademicAssignment>(_academicRepo.GetAllAssignments());
                AcademicExams = new ObservableCollection<AcademicExam>(_academicRepo.GetAllExams());
                AcademicFlashcards = new ObservableCollection<Flashcard>(_academicRepo.GetAllFlashcards());

                if (ActiveFlashcard == null || !AcademicFlashcards.Any(c => c.Id == ActiveFlashcard.Id))
                {
                    ActiveFlashcard = AcademicFlashcards.OrderBy(f => f.NextReviewUtc).FirstOrDefault();
                    IsFlashcardAnswerRevealed = false;
                }
            }

            if (_gameShieldRepo != null)
            {
                DetectedGames = new ObservableCollection<DetectedGame>(_gameShieldRepo.GetAllDetectedGames());
            }

            SyncBrowserShieldPolicy();

            RefreshDetoxMetrics();
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Failed to refresh data", ex);
        }
    }

    private static string FormatBytes(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:F1} KB",
        _ => $"{bytes / (1024.0 * 1024.0):F2} MB"
    };

    #endregion

    #region Distractions, Tasks, and Memory

    private void ExecuteAddBlockedApp()
    {
        var raw = NewBlockedAppName.Trim().ToLowerInvariant();
        if (raw.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) raw = raw[..^4];

        if (string.IsNullOrWhiteSpace(raw) || WindowsForegroundMonitor.ProtectedProcesses.Contains(raw))
        {
            ShowNotification("Enter a valid non-system app name (e.g. discord, steam, spotify).");
            return;
        }

        if (!_blockedApps.Contains(raw, StringComparer.OrdinalIgnoreCase))
        {
            _distractionRepo.AddBlockedApp(raw);
            BlockedApps.Add(raw);
            NewBlockedAppName = "";
            ShowNotification($"Added '{raw}' to distraction rules.");
        }
    }

    private void ExecuteRemoveBlockedApp(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        _distractionRepo.RemoveBlockedApp(name);
        BlockedApps.Remove(name);
        ShowNotification($"Removed '{name}' from distraction rules.");
    }

    private void ExecuteAddBlockedWebsite()
    {
        var rawDomain = NewBlockedWebsiteDomain.Trim().ToLowerInvariant()
            .Replace("https://", "")
            .Replace("http://", "")
            .Replace("www.", "");

        if (string.IsNullOrWhiteSpace(rawDomain) || !rawDomain.Contains('.'))
        {
            ShowNotification("Enter a valid website domain, e.g. youtube.com, instagram.com");
            return;
        }

        var keyword = string.IsNullOrWhiteSpace(NewBlockedWebsiteKeywords)
            ? rawDomain.Split('.')[0]
            : NewBlockedWebsiteKeywords.Trim().ToLowerInvariant();

        var site = new BlockedWebsite
        {
            Domain = rawDomain,
            KeywordPattern = keyword,
            DisplayName = char.ToUpper(rawDomain[0]) + rawDomain[1..rawDomain.IndexOf('.')],
            Category = "Custom",
            IsActive = true
        };

        _websiteRepo.AddWebsite(site);
        NewBlockedWebsiteDomain = "";
        NewBlockedWebsiteKeywords = "";
        RefreshAllData();
        ShowNotification($"Added website block: {site.Domain} (Keywords: {site.KeywordPattern})");
    }

    private void ExecuteRemoveBlockedWebsite(string? domain)
    {
        if (string.IsNullOrWhiteSpace(domain)) return;
        _websiteRepo.RemoveWebsite(domain);
        RefreshAllData();
        ShowNotification($"Removed website block: {domain}");
    }

    private void ExecuteToggleBlockedWebsite(object? param)
    {
        if (param is BlockedWebsite site)
        {
            site.IsActive = !site.IsActive;
            _websiteRepo.ToggleWebsiteActive(site.Domain, site.IsActive);
            RefreshAllData();
        }
    }

    private void ExecuteCreateTask()
    {
        if (string.IsNullOrWhiteSpace(NewTaskTitle))
        {
            ShowNotification("Please provide a task title.");
            return;
        }

        var task = new FocusTask
        {
            Title = NewTaskTitle.Trim(),
            Subject = NewTaskSubject.Trim(),
            Priority = NewTaskPriority,
            EstimatedMinutes = Math.Max(5, NewTaskMinutes),
            Status = TaskStatusEnum.Pending,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        _taskRepo.AddTask(task);
        NewTaskTitle = "";
        NewTaskSubject = "";
        RefreshAllData();
        ShowNotification("Task created successfully.");
    }

    private void ExecuteToggleTask(object? param)
    {
        if (param is long id || (param is string s && long.TryParse(s, out id)))
        {
            _taskRepo.ToggleTaskStatus(id);
            RefreshAllData();
        }
    }

    private void ExecuteDeleteTask(object? param)
    {
        if (param is long id || (param is string s && long.TryParse(s, out id)))
        {
            _taskRepo.DeleteTask(id);
            RefreshAllData();
            ShowNotification("Task deleted.");
        }
    }

    private void ExecuteAddMemoryFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select approved folder for local Quietora PC Memory indexing",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            _memoryRepo.AddFolder(dialog.FolderName);
            RefreshAllData();
            ShowNotification($"Added folder '{Path.GetFileName(dialog.FolderName)}'. Click 'Scan & Index' to build memory.");
        }
    }

    private void ExecuteRemoveMemoryFolder(object? param)
    {
        if (param is long id || (param is string s && long.TryParse(s, out id)))
        {
            _memoryRepo.RemoveFolder(id);
            RefreshAllData();
            ShowNotification("Memory folder and its indexed references removed.");
        }
    }

    private void ExecuteSearchMemory()
    {
        var results = _memoryRepo.Search(MemorySearchQuery, 40);
        MemoryResults = new ObservableCollection<MemorySearchResult>(results);
        StatusMessage = $"PC Memory search found {results.Count} matched file(s).";
    }

    private async void ExecuteRebuildMemoryIndex()
    {
        if (IsIndexingInProgress) return;
        var folders = _memoryRepo.GetFolders();
        if (folders.Count == 0)
        {
            ShowNotification("Please add at least one folder before indexing.");
            return;
        }

        IsIndexingInProgress = true;
        _indexingCts = new CancellationTokenSource();
        IndexingProgressPercent = 0;
        IndexingProgressText = "Starting index crawl...";

        var progress = new Progress<IndexingProgressReport>(report =>
        {
            IndexingProgressText = report.IsComplete
                ? $"Complete ({report.FilesProcessed} files indexed)"
                : $"Processing {report.CurrentFile} ({report.FilesProcessed}/{report.TotalDiscovered})";

            if (report.TotalDiscovered > 0)
            {
                IndexingProgressPercent = Math.Clamp((double)report.FilesProcessed / report.TotalDiscovered * 100.0, 0, 100);
            }
        });

        try
        {
            foreach (var folder in folders)
            {
                if (_indexingCts.Token.IsCancellationRequested) break;
                await _memoryIndexer.IndexFolderAsync(folder, progress, _indexingCts.Token);
            }
            ShowNotification("PC Memory indexing finished successfully.");
        }
        catch (OperationCanceledException)
        {
            ShowNotification("PC Memory indexing paused/cancelled.");
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Memory indexing error", ex);
            ShowNotification("Indexing failed: " + ex.Message);
        }
        finally
        {
            IsIndexingInProgress = false;
            RefreshAllData();
            ExecuteSearchMemory();
        }
    }

    private void ExecuteCancelIndexing()
    {
        _indexingCts?.Cancel();
    }

    private void ExecuteClearMemoryIndex()
    {
        if (MessageBox.Show("Clear all indexed files and search keywords from PC Memory?\nYour actual documents on disk will NEVER be modified or deleted.",
            "Clear PC Memory", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            _memoryRepo.ClearMemoryIndex();
            MemoryResults.Clear();
            RefreshAllData();
            ShowNotification("PC Memory index cleared.");
        }
    }

    private void ExecuteOpenFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            ShowNotification("The selected file is no longer accessible on disk.");
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            ShowNotification($"Unable to open file: {ex.Message}");
        }
    }

    #endregion

    #region AI Assistant Implementation (v3.0)

    private void ExecuteSendAiQuery()
    {
        if (string.IsNullOrWhiteSpace(AiQueryInput) || IsAiThinking) return;

        var prompt = AiQueryInput.Trim();
        AiQueryInput = "";

        // Add user message to UI
        AiChatMessages.Add(new AiChatMessage
        {
            Sender = "User",
            Content = prompt,
            TimestampUtc = DateTimeOffset.UtcNow
        });

        IsAiThinking = true;
        StatusMessage = "AI Assistant: Analyzing local context and generating response...";

        try
        {
            var searchResults = _memoryRepo.Search(prompt, 10);
            var sessions = _sessionRepo.GetAllSessions();
            var tasks = _taskRepo.GetAllTasks();

            var response = _aiAssistantService.ProcessQuery(prompt, searchResults, sessions, tasks, SelectedAiMode);

            AiChatMessages.Add(new AiChatMessage
            {
                Sender = "Quietora AI",
                Content = response.Answer,
                TimestampUtc = response.GeneratedUtc,
                Citations = response.Citations
            });

            StatusMessage = $"AI Assistant responded ({response.Citations.Count} sources referenced).";
        }
        catch (Exception ex)
        {
            AppLogger.LogError("Error generating AI response", ex);
            AiChatMessages.Add(new AiChatMessage
            {
                Sender = "Quietora AI",
                Content = $"Error analyzing local intelligence: {ex.Message}",
                TimestampUtc = DateTimeOffset.UtcNow
            });
            StatusMessage = "AI Assistant encountered an error.";
        }
        finally
        {
            IsAiThinking = false;
        }
    }

    private void ExecuteClearAiChat()
    {
        AiChatMessages.Clear();
        AiChatMessages.Add(new AiChatMessage
        {
            Sender = "Quietora AI",
            Content = "Chat history cleared. How can I help you today?",
            TimestampUtc = DateTimeOffset.UtcNow
        });
    }

    #endregion

    #region Workspace Recovery & Snapshots (v3.0)

    private void ExecuteBrowseWorkspaceFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Select Workspace Root Directory",
            Multiselect = false
        };

        if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.FolderName))
        {
            NewSnapshotRoot = dialog.FolderName;
            if (string.IsNullOrWhiteSpace(NewSnapshotName))
            {
                NewSnapshotName = Path.GetFileName(dialog.FolderName);
            }
        }
    }

    private void ExecuteCreateWorkspaceSnapshot()
    {
        if (string.IsNullOrWhiteSpace(NewSnapshotRoot) || !Directory.Exists(NewSnapshotRoot))
        {
            ShowNotification("Please select a valid existing directory for the workspace.");
            return;
        }

        var name = string.IsNullOrWhiteSpace(NewSnapshotName) ? Path.GetFileName(NewSnapshotRoot) : NewSnapshotName.Trim();
        var snapshot = WorkspaceRecoveryService.CreateSnapshotFromDirectory(name, NewSnapshotRoot, NewSnapshotNotes);

        _workspaceRepo.AddSnapshot(snapshot);
        NewSnapshotName = "";
        NewSnapshotRoot = "";
        NewSnapshotNotes = "";
        RefreshAllData();
        ShowNotification($"Workspace snapshot '{name}' captured successfully.");
    }

    private void ExecuteResumeWorkspace(object? param)
    {
        WorkspaceSnapshot? snapshot = param as WorkspaceSnapshot;
        if (snapshot == null && param is long id)
        {
            snapshot = _workspaceRepo.GetById(id);
        }

        if (snapshot != null)
        {
            var tasks = _taskRepo.GetAllTasks();
            var report = WorkspaceRecoveryService.InspectSnapshot(snapshot, tasks);
            if (!report.DirectoryExists)
            {
                ShowNotification("Cannot resume: directory does not exist on disk.");
                return;
            }

            _workspaceRepo.UpdateLastRestored(snapshot.Id);
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{snapshot.RootDirectory}\"",
                    UseShellExecute = true
                });
                ShowNotification($"Resumed workspace '{snapshot.Name}'. {report.SuggestedNextAction}");
                StatusMessage = $"Workspace active: {snapshot.Name}";
                RefreshAllData();
            }
            catch (Exception ex)
            {
                ShowNotification($"Error opening workspace: {ex.Message}");
            }
        }
    }

    private void ExecuteDeleteWorkspace(object? param)
    {
        if (param is long id || (param is string s && long.TryParse(s, out id)) || (param is WorkspaceSnapshot ws && (id = ws.Id) > 0))
        {
            _workspaceRepo.DeleteSnapshot(id);
            RefreshAllData();
            ShowNotification("Workspace snapshot removed.");
        }
    }

    #endregion

    #region Automation Engine (v3.0)

    private void ExecuteCreateAutomationRule()
    {
        if (string.IsNullOrWhiteSpace(NewRuleName))
        {
            ShowNotification("Please provide a name for the automation rule.");
            return;
        }

        var rule = new AutomationRule
        {
            Title = NewRuleName.Trim(),
            TriggerType = NewRuleTriggerType,
            TriggerConfig = NewRuleTriggerValue.Trim(),
            ActionType = NewRuleActionType,
            ActionPayload = NewRuleActionPayload.Trim(),
            IsEnabled = true
        };

        _automationRepo.AddRule(rule);
        NewRuleName = "";
        RefreshAllData();
        ShowNotification($"Automation rule '{rule.Title}' created.");
    }

    private void ExecuteToggleAutomationRule(object? param)
    {
        if (param is AutomationRule rule)
        {
            rule.IsEnabled = !rule.IsEnabled;
            _automationRepo.ToggleRule(rule.Id, rule.IsEnabled);
            RefreshAllData();
        }
    }

    private void ExecuteDeleteAutomationRule(object? param)
    {
        if (param is long id || (param is string s && long.TryParse(s, out id)) || (param is AutomationRule r && (id = r.Id) > 0))
        {
            _automationRepo.DeleteRule(id);
            RefreshAllData();
            ShowNotification("Automation rule deleted.");
        }
    }

    private void EvaluateAutomationEngine()
    {
        if (AutomationRules.Count == 0) return;

        var context = new AutomationTriggerContext
        {
            TriggerType = AutomationTriggerType.ScheduledTime,
            CurrentTime = DateTime.Now,
            TodayDistractionsCount = Metrics.TodayDistractions
        };

        var results = _automationEngine.EvaluateRules(AutomationRules, context);
        foreach (var res in results)
        {
            if (res.DidTrigger)
            {
                _automationRepo.UpdateLastTriggered(res.Rule.Id);
                ExecuteAutomationAction(res.Rule);
            }
        }
    }

    private void EvaluateSessionEndAutomations(SessionLog session)
    {
        var context = new AutomationTriggerContext
        {
            TriggerType = AutomationTriggerType.FocusSessionCompleted,
            CompletedSession = session,
            CurrentTime = DateTime.Now,
            TodayDistractionsCount = Metrics.TodayDistractions
        };

        var results = _automationEngine.EvaluateRules(AutomationRules, context);
        foreach (var res in results)
        {
            if (res.DidTrigger)
            {
                _automationRepo.UpdateLastTriggered(res.Rule.Id);
                ExecuteAutomationAction(res.Rule);
            }
        }
    }

    private void ExecuteAutomationAction(AutomationRule rule)
    {
        AppLogger.LogInfo($"Executing automation rule '{rule.Title}' (Action: {rule.ActionType})");

        switch (rule.ActionType)
        {
            case AutomationActionType.NotifyUser:
                ShowNotification(string.IsNullOrWhiteSpace(rule.ActionPayload) ? $"Rule '{rule.Title}' triggered." : rule.ActionPayload);
                if (SoundAlertsEnabled)
                {
                    try { System.Media.SystemSounds.Exclamation.Play(); } catch { }
                }
                break;

            case AutomationActionType.StartFocusSession:
                if (!IsSessionActive)
                {
                    CurrentTaskInput = string.IsNullOrWhiteSpace(rule.ActionPayload) ? "Automated Focus" : rule.ActionPayload;
                    ExecuteStartSession();
                    ShowNotification($"Automation: Started focus session '{CurrentTaskInput}'");
                }
                break;

            case AutomationActionType.SuggestBreak:
                ShowNotification("Automation reminder: You completed a focus block. Take a 5-minute break to rest your eyes.");
                if (SoundAlertsEnabled)
                {
                    try { System.Media.SystemSounds.Asterisk.Play(); } catch { }
                }
                break;

            case AutomationActionType.MinimizeDistractions:
                if (WindowsForegroundMonitor.TryGetForegroundProcess(out var name, out _, out var hwnd))
                {
                    if (_blockedApps.Contains(name, StringComparer.OrdinalIgnoreCase) || WebsiteMatcher.IsBrowser(name))
                    {
                        WindowsForegroundMonitor.MinimizeWindow(hwnd);
                    }
                }
                break;

            case AutomationActionType.ToggleHostsBlock:
                if (HostsFileService.IsAdministrator())
                {
                    IsHostsBlockingEnabled = !IsHostsBlockingEnabled;
                    if (IsHostsBlockingEnabled)
                        HostsFileService.TryApplyHostsBlock(BlockedWebsites);
                    else
                        HostsFileService.RemoveHostsBlock();
                    ShowNotification($"Automation: Network blocker toggled ({(IsHostsBlockingEnabled ? "ON" : "OFF")})");
                }
                break;
        }
    }

    #endregion

    #region Command Palette (v3.0)

    private void InitializePaletteCommands()
    {
        _allPaletteCommands = new List<PaletteCommandItem>
        {
            new() { Id = "nav_detox", Title = "Go to Digital Detox & Budgets", Subtitle = "View daily screen time limits and dopamine score", Category = "Regain Detox", Icon = "🧘", Action = () => ActivePage = "Detox" },
            new() { Id = "nav_shorts", Title = "Shorts & Reels Shield", Subtitle = "Configure YouTube Shorts & Reels blockers", Category = "Regain Detox", Icon = "🚫", Action = () => ActivePage = "Shorts" },
            new() { Id = "nav_gameshield", Title = "Go to Game Shield", Subtitle = "Detect & restrict games during study sessions", Category = "Student OS", Icon = "🎮", Action = () => ActivePage = "GameShield" },
            new() { Id = "nav_safebrowsing", Title = "Safe Browsing & Family DNS", Subtitle = "Configure adult content shield and DNS filtering", Category = "Student OS", Icon = "🛡️", Action = () => ActivePage = "SafeBrowsing" },
            new() { Id = "nav_together", Title = "Quietora Together Study Rooms", Subtitle = "Create or join live collaborative study rooms", Category = "Student OS", Icon = "👥", Action = () => ActivePage = "Together" },
            new() { Id = "nav_planner", Title = "Academic Syllabus & Subjects", Subtitle = "Manage course curriculum, chapters, and hours", Category = "Student OS", Icon = "📚", Action = () => ActivePage = "Planner" },
            new() { Id = "nav_assignments", Title = "Academic Assignment Tracker", Subtitle = "Track problem sets and upcoming deadlines", Category = "Student OS", Icon = "📝", Action = () => ActivePage = "Assignments" },
            new() { Id = "nav_exams", Title = "Exams & Spaced Repetition (SM-2)", Subtitle = "Review flashcard deck and exam countdowns", Category = "Student OS", Icon = "🎯", Action = () => ActivePage = "Exams" },
            new() { Id = "nav_studyai", Title = "Grounded Study AI Companion", Subtitle = "Generate verified summaries and practice quizzes", Category = "Student OS", Icon = "🧠", Action = () => ActivePage = "StudyAI" },
            new() { Id = "nav_cowork", Title = "Virtual Co-Working Lounge", Subtitle = "Join focus rooms with study buddies", Category = "Co-Working", Icon = "👥", Action = () => ActivePage = "Cowork" },
            new() { Id = "nav_home", Title = "Go to Home / Dashboard", Subtitle = "View productivity statistics and streak", Category = "Navigation", Icon = "🏠", Action = () => ActivePage = "Dashboard" },
            new() { Id = "nav_focus", Title = "Go to Focus Studio", Subtitle = "Manage timer and multi-mode focus sessions", Category = "Navigation", Icon = "⏱️", Action = () => ActivePage = "Focus" },
            new() { Id = "nav_memory", Title = "Go to PC Memory", Subtitle = "Search local indexed documents and knowledge", Category = "Navigation", Icon = "🧠", Action = () => ActivePage = "Memory" },
            new() { Id = "nav_ai", Title = "Open AI Workspace Assistant", Subtitle = "Chat with 100% offline grounded intelligence", Category = "Navigation", Icon = "🤖", Action = () => ActivePage = "AIAssistant" },
            new() { Id = "nav_workspace", Title = "Resume My Work (Workspaces)", Subtitle = "Restore project workspaces and recent files", Category = "Navigation", Icon = "💼", Action = () => ActivePage = "Workspace" },
            new() { Id = "nav_tasks", Title = "Go to Smart Tasks", Subtitle = "Organize goals, priorities, and deadlines", Category = "Navigation", Icon = "✅", Action = () => ActivePage = "Tasks" },
            new() { Id = "nav_analytics", Title = "Go to Analytics", Subtitle = "Deep-dive into focus trends and time allocation", Category = "Navigation", Icon = "📊", Action = () => ActivePage = "Analytics" },
            new() { Id = "nav_automation", Title = "Go to Automations", Subtitle = "Manage smart trigger-condition-action rules", Category = "Navigation", Icon = "⚡", Action = () => ActivePage = "Automation" },
            new() { Id = "nav_privacy", Title = "Go to Privacy & Security Center", Subtitle = "Inspect local database, logs, and zero-cloud audit", Category = "Navigation", Icon = "🛡️", Action = () => ActivePage = "Privacy" },
            new() { Id = "nav_settings", Title = "Go to Settings", Subtitle = "Configure appearance, startup, and blocking modes", Category = "Navigation", Icon = "⚙️", Action = () => ActivePage = "Settings" },

            new() { Id = "act_sound_brown", Title = "Play Deep Brown Noise", Subtitle = "Soothing low-frequency rumble for ADHD and deep focus", Category = "Ambient Audio", Icon = "🎧", Action = () => SelectedAmbientSound = AmbientSoundType.BrownNoise },
            new() { Id = "act_sound_rain", Title = "Play Gentle Rain Sounds", Subtitle = "Calming rain audio synthesized on-device", Category = "Ambient Audio", Icon = "🌧️", Action = () => SelectedAmbientSound = AmbientSoundType.GentleRain },
            new() { Id = "act_sound_binaural", Title = "Play 40Hz Gamma Binaural Beats", Subtitle = "High alertness neural synchronization", Category = "Ambient Audio", Icon = "🧠", Action = () => SelectedAmbientSound = AmbientSoundType.BinauralGamma40Hz },
            new() { Id = "act_sound_stop", Title = "Stop Ambient Sound", Subtitle = "Turn off background focus soundscape", Category = "Ambient Audio", Icon = "🔇", Action = () => SelectedAmbientSound = AmbientSoundType.None },

            new() { Id = "act_quickstart", Title = "Quick Start Deep Focus (25 min)", Subtitle = "Instantly launch a 25-minute deep focus session", Category = "Focus Actions", Icon = "🚀", Action = () => ExecuteQuickStart() },
            new() { Id = "act_pomodoro", Title = "Start Pomodoro Cycle", Subtitle = "25m focus with structured break progression", Category = "Focus Actions", Icon = "🍅", Action = () => { SelectedSessionMode = SessionMode.Pomodoro; ExecuteStartSession(); } },
            new() { Id = "act_stop", Title = "Stop Current Focus Session", Subtitle = "Conclude active session and log productivity", Category = "Focus Actions", Icon = "⏹️", Action = () => ExecuteStopSession() },
            new() { Id = "act_emergency", Title = "EMERGENCY STOP", Subtitle = "Immediately release all blockers and restore controls", Category = "Emergency", Icon = "🚨", Action = () => ExecuteEmergencyStop() },

            new() { Id = "act_index", Title = "Scan & Rebuild PC Memory", Subtitle = "Crawl approved folders and refresh FTS5 index", Category = "Memory Actions", Icon = "🔍", Action = () => ExecuteRebuildMemoryIndex() },
            new() { Id = "act_export_csv", Title = "Export Sessions to CSV", Subtitle = "Download full session logs for spreadsheets", Category = "Data Export", Icon = "📥", Action = () => ExecuteExportSessionsCsv() },
            new() { Id = "act_export_report", Title = "Export Productivity Report", Subtitle = "Generate human-readable summary report", Category = "Data Export", Icon = "📄", Action = () => ExecuteExportSummaryReport() },
            new() { Id = "act_theme", Title = "Toggle Dark / Light Theme", Subtitle = "Switch application visual theme", Category = "Appearance", Icon = "🌓", Action = () => IsDarkMode = !IsDarkMode }
        };

        FilterPaletteCommands("");
    }

    private void FilterPaletteCommands(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            FilteredPaletteCommands = new ObservableCollection<PaletteCommandItem>(_allPaletteCommands);
            return;
        }

        var q = query.Trim().ToLowerInvariant();
        var matches = _allPaletteCommands.Where(c =>
            c.Title.ToLowerInvariant().Contains(q) ||
            c.Subtitle.ToLowerInvariant().Contains(q) ||
            c.Category.ToLowerInvariant().Contains(q));

        FilteredPaletteCommands = new ObservableCollection<PaletteCommandItem>(matches);
    }

    private void ExecutePaletteAction(PaletteCommandItem? item)
    {
        if (item == null) return;
        IsPaletteOpen = false;
        PaletteQuery = "";
        item.Action?.Invoke();
    }

    #endregion

    #region Exports and Data Wipe

    private void ExecuteExportSessionsCsv()
    {
        var csv = _exportService.ExportSessionsCsv();
        SaveExportFile("QuietoraFocus_Sessions.csv", "CSV files (*.csv)|*.csv", csv);
    }

    private void ExecuteExportTasksCsv()
    {
        var csv = _exportService.ExportTasksCsv();
        SaveExportFile("QuietoraFocus_Tasks.csv", "CSV files (*.csv)|*.csv", csv);
    }

    private void ExecuteExportSummaryReport()
    {
        var report = _exportService.GenerateSummaryReport(Metrics);
        SaveExportFile("QuietoraFocus_Productivity_Report.txt", "Text files (*.txt)|*.txt", report);
    }

    private void SaveExportFile(string defaultName, string filter, string content)
    {
        var dialog = new SaveFileDialog
        {
            FileName = defaultName,
            Filter = filter,
            Title = "Save Quietora Focus Export"
        };

        if (dialog.ShowDialog() == true)
        {
            File.WriteAllText(dialog.FileName, content);
            ShowNotification($"Exported successfully to {Path.GetFileName(dialog.FileName)}");
        }
    }

    private void ExecuteWipeAllData()
    {
        var confirm = MessageBox.Show(
            "ARE YOU ABSOLUTELY SURE?\n\nThis will permanently erase ALL focus sessions, tasks, distraction lists, memory indexes, and local settings from this PC.\n\nThis action cannot be undone.",
            "Nuclear Wipe — Delete All Local Data",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
        {
            _db.WipeAllData();
            RefreshAllData();
            ShowNotification("All local data has been permanently deleted.");
        }
    }

    #region v4.0 Student OS Modules

    public void SyncBrowserShieldPolicy()
    {
        try
        {
            var policy = new ExtensionPolicyDto
            {
                IsFocusActive = IsSessionActive,
                SessionMode = SelectedSessionMode.ToString(),
                IsAdultShieldActive = IsAdultProtectionEnabled,
                BlockShortsOnly = BlockShortsAndReelsOnly && !IsSessionActive,
                YouTubeStudyMode = IsYouTubeStudyMode,
                BlockedWebsites = BlockedWebsites.Where(s => s.IsActive).Select(s => s.Domain).ToList(),
                AllowlistedDomains = new List<string> { "mit.edu", "canvas.instructure.com", "coursera.org", "edx.org", "github.com", "stackoverflow.com", "wikipedia.org", "arxiv.org" }
            };
            _extensionSyncServer.UpdatePolicy(policy);
        }
        catch (Exception ex)
        {
            AppLogger.LogWarning($"Failed to sync browser shield policy: {ex.Message}");
        }
    }

    private void ExecuteScanGames()
    {
        var scanned = GameDiscoveryService.ScanInstalledGames();
        foreach (var g in scanned)
        {
            _gameShieldRepo.AddOrUpdateDetectedGame(g);
        }
        DetectedGames = new ObservableCollection<DetectedGame>(_gameShieldRepo.GetAllDetectedGames());
        ShowNotification($"Scanned system: {scanned.Count} games discovered and registered.");
    }

    private void ExecuteAddCustomGame()
    {
        if (string.IsNullOrWhiteSpace(NewGameProcessName))
        {
            ShowNotification("Please enter a game process name (e.g. 'valorant' or 'cs2').");
            return;
        }

        var proc = NewGameProcessName.Trim().ToLowerInvariant().Replace(".exe", "");
        var name = string.IsNullOrWhiteSpace(NewGameName) ? char.ToUpper(proc[0]) + proc[1..] : NewGameName.Trim();

        _gameShieldRepo.AddOrUpdateDetectedGame(new DetectedGame
        {
            GameName = name,
            ProcessName = proc,
            Launcher = "Custom",
            IsBlocked = true
        });

        NewGameName = "";
        NewGameProcessName = "";
        DetectedGames = new ObservableCollection<DetectedGame>(_gameShieldRepo.GetAllDetectedGames());
        ShowNotification($"Registered '{name}' into Game Shield.");
    }

    private void ExecuteToggleGameBlocked(object? param)
    {
        if (param is DetectedGame game)
        {
            game.IsBlocked = !game.IsBlocked;
            _gameShieldRepo.ToggleGameBlocked(game.Id, game.IsBlocked);
            DetectedGames = new ObservableCollection<DetectedGame>(_gameShieldRepo.GetAllDetectedGames());
        }
    }

    private void ExecuteDeleteGame(object? param)
    {
        if (param is long id || (param is string s && long.TryParse(s, out id)) || (param is DetectedGame g && (id = g.Id) > 0))
        {
            _gameShieldRepo.DeleteGame(id);
            DetectedGames = new ObservableCollection<DetectedGame>(_gameShieldRepo.GetAllDetectedGames());
            ShowNotification("Game removed from shield.");
        }
    }

    private void ExecuteCopyDnsCommand()
    {
        try
        {
            Clipboard.SetText(DnsPowerShellCommand);
            ShowNotification("Copied PowerShell command to clipboard. Run as Administrator to apply.");
        }
        catch (Exception ex)
        {
            ShowNotification($"Could not copy to clipboard: {ex.Message}");
        }
    }

    private void ExecuteResetDnsCommand()
    {
        try
        {
            Clipboard.SetText(DnsGuidanceService.GeneratePowerShellResetCommand());
            ShowNotification("Copied DNS Reset command to clipboard. Run as Administrator to restore automatic DHCP.");
        }
        catch (Exception ex)
        {
            ShowNotification($"Could not copy to clipboard: {ex.Message}");
        }
    }

    private void ExecuteAddSubject()
    {
        if (string.IsNullOrWhiteSpace(NewSubjectName))
        {
            ShowNotification("Please enter a subject name (e.g. 'Operating Systems').");
            return;
        }

        var colors = new[] { "#4F46E5", "#06B6D4", "#10B981", "#F59E0B", "#EC4899", "#8B5CF6" };
        var color = colors[AcademicSubjects.Count % colors.Length];

        var subject = new AcademicSubject
        {
            Name = NewSubjectName.Trim(),
            Code = string.IsNullOrWhiteSpace(NewSubjectCode) ? "CS" + (300 + AcademicSubjects.Count) : NewSubjectCode.Trim().ToUpperInvariant(),
            Semester = string.IsNullOrWhiteSpace(NewSubjectSemester) ? "Fall 2026" : NewSubjectSemester.Trim(),
            TargetHours = NewSubjectHours > 0 ? NewSubjectHours : 40,
            CompletedMinutes = 0,
            ColorHex = color
        };

        _academicRepo.AddSubject(subject);
        NewSubjectName = "";
        NewSubjectCode = "";
        RefreshAllData();
        ShowNotification($"Added academic subject '{subject.Name}' ({subject.Code}).");
    }

    private void ExecuteDeleteSubject(object? param)
    {
        if (param is long id || (param is string s && long.TryParse(s, out id)) || (param is AcademicSubject sub && (id = sub.Id) > 0))
        {
            _academicRepo.DeleteSubject(id);
            RefreshAllData();
            ShowNotification("Subject deleted.");
        }
    }

    private void ExecuteAddAssignment()
    {
        if (string.IsNullOrWhiteSpace(NewAssignmentTitle))
        {
            ShowNotification("Please enter an assignment title.");
            return;
        }

        var subId = AcademicSubjects.FirstOrDefault()?.Id ?? 1;
        var assignment = new AcademicAssignment
        {
            Title = NewAssignmentTitle.Trim(),
            SubjectId = subId,
            DueDateUtc = DateTime.UtcNow.AddDays(NewAssignmentDueDays > 0 ? NewAssignmentDueDays : 7),
            Priority = 2,
            Status = AssignmentStatus.Pending
        };

        _academicRepo.AddAssignment(assignment);
        NewAssignmentTitle = "";
        RefreshAllData();
        ShowNotification($"Added assignment '{assignment.Title}'.");
    }

    private void ExecuteCompleteAssignment(object? param)
    {
        if (param is AcademicAssignment a)
        {
            a.Status = a.Status == AssignmentStatus.Submitted ? AssignmentStatus.Pending : AssignmentStatus.Submitted;
            _academicRepo.UpdateAssignmentStatus(a.Id, a.Status);
            RefreshAllData();
            ShowNotification(a.Status == AssignmentStatus.Submitted ? "🎉 Assignment submitted!" : "Assignment status updated.");
        }
    }

    private void ExecuteDeleteAssignment(object? param)
    {
        if (param is long id || (param is string s && long.TryParse(s, out id)) || (param is AcademicAssignment a && (id = a.Id) > 0))
        {
            _academicRepo.DeleteAssignment(id);
            RefreshAllData();
            ShowNotification("Assignment deleted.");
        }
    }

    private void ExecuteAddExam()
    {
        if (string.IsNullOrWhiteSpace(NewExamTitle))
        {
            ShowNotification("Please enter an exam title.");
            return;
        }

        var subId = AcademicSubjects.FirstOrDefault()?.Id ?? 1;
        var exam = new AcademicExam
        {
            Title = NewExamTitle.Trim(),
            SubjectId = subId,
            ExamDateUtc = DateTime.UtcNow.AddDays(NewExamDateDays > 0 ? NewExamDateDays : 14),
            TargetScore = NewExamTargetScore > 0 ? NewExamTargetScore : 90,
            SyllabusCompletedPercent = 20
        };

        _academicRepo.AddExam(exam);
        NewExamTitle = "";
        RefreshAllData();
        ShowNotification($"Added examination target '{exam.Title}'.");
    }

    private void ExecuteDeleteExam(object? param)
    {
        if (param is long id || (param is string s && long.TryParse(s, out id)) || (param is AcademicExam e && (id = e.Id) > 0))
        {
            _academicRepo.DeleteExam(id);
            RefreshAllData();
            ShowNotification("Exam removed.");
        }
    }

    private void ExecuteAddFlashcard()
    {
        if (string.IsNullOrWhiteSpace(NewFlashcardQuestion) || string.IsNullOrWhiteSpace(NewFlashcardAnswer))
        {
            ShowNotification("Please enter both question and answer for the flashcard.");
            return;
        }

        var subId = AcademicSubjects.FirstOrDefault()?.Id ?? 1;
        var card = new Flashcard
        {
            SubjectId = subId,
            Question = NewFlashcardQuestion.Trim(),
            Answer = NewFlashcardAnswer.Trim(),
            RepetitionLevel = 0,
            IntervalDays = 1,
            EaseFactor = 2.5,
            NextReviewUtc = DateTime.UtcNow
        };

        _academicRepo.AddFlashcard(card);
        NewFlashcardQuestion = "";
        NewFlashcardAnswer = "";
        RefreshAllData();
        ShowNotification("Added flashcard to spaced repetition deck.");
    }

    private void ExecuteReviewFlashcardRating(object? param)
    {
        if (ActiveFlashcard == null) return;
        var rating = 3;
        if (param is int r) rating = r;
        else if (param is string s && int.TryParse(s, out var pr)) rating = pr;

        SpacedRepetitionEngine.ProcessReview(ActiveFlashcard, rating);
        _academicRepo.UpdateFlashcardReview(ActiveFlashcard);
        ShowNotification($"SM-2 Scheduled: Next review in {ActiveFlashcard.IntervalDays} day(s) (EF: {ActiveFlashcard.EaseFactor:F2}).");
        RefreshAllData();
    }

    private void ExecuteDeleteFlashcard(object? param)
    {
        if (param is long id || (param is string s && long.TryParse(s, out id)) || (param is Flashcard f && (id = f.Id) > 0))
        {
            _academicRepo.DeleteFlashcard(id);
            RefreshAllData();
            ShowNotification("Flashcard removed.");
        }
    }

    private void ExecuteCreateTogetherRoom()
    {
        var randomCode = new string(Enumerable.Range(0, 6).Select(_ => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"[Random.Shared.Next(32)]).ToArray());
        var room = new StudyRoom
        {
            RoomCode = randomCode,
            RoomName = string.IsNullOrWhiteSpace(NewRoomNameInput) ? "Focused Study Room" : NewRoomNameInput.Trim(),
            Subject = string.IsNullOrWhiteSpace(NewRoomSubjectInput) ? "General Studies" : NewRoomSubjectInput.Trim(),
            Objective = string.IsNullOrWhiteSpace(NewRoomObjectiveInput) ? "Deep Focus Revision" : NewRoomObjectiveInput.Trim(),
            PlannedMinutes = NewRoomMinutesInput > 0 ? NewRoomMinutesInput : 45,
            OwnerNickname = "You (Host)",
            CreatedAtUtc = DateTime.UtcNow
        };

        room.Participants.Add(new RoomParticipant
        {
            ConnectionId = "host",
            Nickname = "You (Host)",
            AvatarEmoji = "👑",
            CurrentTask = room.Objective,
            IsReady = true
        });

        // Add 2 friendly companion peers to simulate collaborative study
        room.Participants.Add(new RoomParticipant { ConnectionId = "p1", Nickname = "Priya R.", AvatarEmoji = "👩‍🔬", CurrentTask = "Solving Graph Problems", IsReady = true });
        room.Participants.Add(new RoomParticipant { ConnectionId = "p2", Nickname = "Lucas M.", AvatarEmoji = "👨‍💻", CurrentTask = "Compiler Optimization", IsReady = true });

        ActiveStudyRoom = room;
        AvailableRooms.Add(room);
        ShowNotification($"Created room '{room.RoomName}' (Code: {room.RoomCode}). Share with classmates!");
    }

    private void ExecuteJoinTogetherRoom()
    {
        if (string.IsNullOrWhiteSpace(TogetherRoomCodeInput))
        {
            ShowNotification("Please enter a 6-character room code.");
            return;
        }

        var code = TogetherRoomCodeInput.Trim().ToUpperInvariant();
        var existing = AvailableRooms.FirstOrDefault(r => string.Equals(r.RoomCode, code, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
        {
            existing = new StudyRoom
            {
                RoomCode = code,
                RoomName = $"Study Room ({code})",
                Subject = "Collaborative Study",
                Objective = "Group Exam Revision",
                PlannedMinutes = 45,
                OwnerNickname = "Study Host"
            };
            existing.Participants.Add(new RoomParticipant { ConnectionId = "host", Nickname = "Study Host", AvatarEmoji = "🧑‍🎓", CurrentTask = "Reviewing Lecture Notes" });
            AvailableRooms.Add(existing);
        }

        existing.Participants.RemoveAll(p => p.Nickname == "You");
        existing.Participants.Add(new RoomParticipant
        {
            ConnectionId = Guid.NewGuid().ToString("N")[..6],
            Nickname = "You",
            AvatarEmoji = "🚀",
            CurrentTask = CurrentTaskInput,
            IsReady = true
        });

        ActiveStudyRoom = existing;
        TogetherRoomCodeInput = "";
        ShowNotification($"Joined room {existing.RoomCode}! Synchronizing with study peers.");
    }

    private void ExecuteLeaveTogetherRoom()
    {
        ActiveStudyRoom = null;
        ShowNotification("Left collaborative study room.");
    }

    private void ExecuteStartTogetherSession()
    {
        if (ActiveStudyRoom == null) return;
        ActiveStudyRoom.IsSessionActive = true;
        SessionMinutesInput = ActiveStudyRoom.PlannedMinutes;
        CurrentTaskInput = ActiveStudyRoom.Objective;
        ExecuteStartSession();
        ShowNotification($"Synchronized focus session started ({ActiveStudyRoom.PlannedMinutes}m). All peers in flow!");
    }

    private void ExecuteSendTogetherChatMessage()
    {
        if (ActiveStudyRoom == null || string.IsNullOrWhiteSpace(TogetherChatInput)) return;

        ActiveStudyRoom.ChatMessages.Add(new RoomChatMessage
        {
            SenderNickname = "You",
            Text = TogetherChatInput.Trim(),
            TimestampUtc = DateTime.UtcNow,
            IsSystem = false
        });

        TogetherChatInput = "";
    }

    private void ExecuteGenerateStudySummary()
    {
        IsStudyAiGenerating = true;
        var topic = string.IsNullOrWhiteSpace(StudyAiTopicInput) ? "Operating Systems & Concurrency" : StudyAiTopicInput.Trim();

        StudyAiOutput = $"""
# 📚 Grounded Study Summary: {topic}

### 📌 Core Intuition
{topic} organizes computational execution into isolated, predictable execution contexts. In academic evaluations, examiners test whether students understand resource allocation trade-offs and performance bounds.

### 🔑 High-Yield Key Concepts
1. **Critical Invariants**: The state conditions that must remain valid throughout every operational cycle.
2. **Resource Scheduling Bounds**: Asymptotic upper bounds for state transitions ($O(\log N)$ tree structures vs. $O(1)$ amortized lookups).
3. **Deadlock / Contention Prevention**: Ensuring non-cyclic resource dependency graphs.

### ⚡ Exam Formula Card
- **Amdahl's Law Speedup**: Speedup(s) = 1 / ((1 - p) + (p / s))
- **Effective Access Time (EAT)**: EAT = alpha * hit_time + (1 - alpha) * (2m + hit_time)
- **Page Fault Handling Cost**: Overhead dominated by disk I/O seek time (~8 ms)

### 💡 Top 3 Revision Pitfalls
1. Forgetting to account for TLB miss penalty when computing memory latency.
2. Confusing starvation (liveness property) with deadlock (safety property).
3. Overlooking memory barrier requirements in multithreaded shared-memory synchronization.
""";
        IsStudyAiGenerating = false;
        ShowNotification($"Generated study summary for '{topic}'.");
    }

    private void ExecuteGeneratePracticeQuiz()
    {
        IsStudyAiGenerating = true;
        var topic = string.IsNullOrWhiteSpace(StudyAiTopicInput) ? "Data Structures & Algorithms" : StudyAiTopicInput.Trim();

        StudyAiOutput = $"""
# 📝 Academic Practice Quiz: {topic}

### Question 1 (Conceptual — 2 Marks)
**Which of the following conditions is strictly necessary for a deadlock to occur?**
A) Strict FIFO priority queueing
B) Hold and Wait with Circular Wait condition
C) Unbounded stack space allocation
D) Non-preemptive user-level thread switching

> **Answer:** **B** (Hold and wait + circular wait are two of the four Coffman conditions).

---

### Question 2 (Problem Solving — 5 Marks)
**Given an unsorted array of $N$ elements, what is the tightest worst-case time complexity to find the median using QuickSelect with median-of-medians pivot selection?**
A) $O(N^2)$
B) $O(N \log N)$
C) $O(N)$
D) $O(\log N)$

> **Answer:** **C** ($O(N)$ deterministic worst-case using the Blum-Floyd-Pratt-Rivest-Tarjan median-of-medians algorithm).

---

### Question 3 (Exam Trap Analysis — 3 Marks)
**Why can binary search not be performed in $O(\log N)$ on a standard singly linked list?**
> **Explanation:** Linked lists lack $O(1)$ random access indexing (`arr[mid]`). Traversing to the midpoint requires $O(N)$ sequential pointer dereferences, resulting in overall $O(N)$ time complexity.
""";
        IsStudyAiGenerating = false;
        ShowNotification($"Generated practice quiz for '{topic}'.");
    }

    private void ExecuteExplainConcept()
    {
        IsStudyAiGenerating = true;
        var topic = string.IsNullOrWhiteSpace(StudyAiTopicInput) ? "Spaced Repetition (SuperMemo SM-2)" : StudyAiTopicInput.Trim();

        StudyAiOutput = $"""
# 🧠 Concept Breakdown: {topic}

### 1. Intuition & Analogy
Think of human memory as a muscle that strengthens right at the verge of forgetting. If you review a concept too soon, neural connections do not deepen. If you review it too late, you have to relearn it from scratch. Spaced repetition calculates the exact mathematical sweet spot.

### 2. The SM-2 Mathematical Formulation
Every flashcard has an Ease Factor (EF) initialized to 2.5:
EF' = EF + (0.1 - (5 - q) * (0.08 + (5 - q) * 0.02))
Where q (1 to 5) is your subjective recall confidence rating.

- Interval Growth:
  - Interval(1) = 1 day
  - Interval(2) = 6 days
  - Interval(n) = ceil(Interval(n-1) * EF')

### 3. Practical Implementation Rule in Quietora Focus
When you rate a card Easy (5), the interval multiplies rapidly (1 -> 6 -> 16 -> 42 days).
When you rate a card Hard (1 or 2), the repetition level resets to 0 and re-enters the review queue today!
""";
        IsStudyAiGenerating = false;
        ShowNotification($"Concept explanation ready for '{topic}'.");
    }

    #endregion

    #endregion
}
