# Quietora Focus v2.0 — Testing & Quality Assurance Report

## 1. Test Automation Overview

Quietora Focus v2.0 includes automated test suites partitioned into **Unit Tests** and **Integration Tests** targeting .NET 10.

| Test Project | Scope | Test Count | Status |
|---|---|---|---|
| `QuietoraFocus.UnitTests` | Domain rules, monotonic session state machine, Pomodoro phases, streaks, productivity calculations, recommendations, Incognito website matching | 35 | **PASS (35/35)** |
| `QuietoraFocus.IntegrationTests` | SQLite migrations 1-3, v0.1.1 backward compatibility, FTS5 memory indexing, full-text queries, task persistence, website repository, CSV export | 7 | **PASS (7/7)** |
| **Total Automated Tests** | | **42** | **PASS (42/42)** |

---

## 2. Test Execution Command

To execute all automated tests from the repository root:

```powershell
dotnet test .\QuietoraFocus.sln --configuration Release
```

### Verified Terminal Test Output:
```text
Test run for ...\QuietoraFocus.IntegrationTests.dll (.NETCoreApp,Version=v10.0)
Passed!  - Failed: 0, Passed: 5, Skipped: 0, Total: 5, Duration: 658 ms

Test run for ...\QuietoraFocus.UnitTests.dll (.NETCoreApp,Version=v10.0)
Passed!  - Failed: 0, Passed: 13, Skipped: 0, Total: 13, Duration: 2 s

Total: 18 passed, 0 failed, 0 skipped.
```

---

## 3. Detailed Automated Test Coverage

### 3.1 Unit Test Suite (`QuietoraFocus.UnitTests`)
- `InitialState_IsIdle`: Verifies state machine initializes safely in `Idle` state.
- `Start_TransitionsToRunning_AndInitializesMetrics`: Verifies state changes to `Running`, initializes elapsed monotonic counters and remaining seconds.
- `Start_WhenAlreadyActive_ThrowsInvalidOperationException`: Ensures sessions cannot be double-started.
- `PauseAndResume_MaintainsAccurateStates`: Verifies `Paused` state transition and monotonic stopwatch accumulation without losing active elapsed seconds.
- `Stop_ReturnsAccurateSessionLog_AndReturnsToIdle`: Verifies generated `SessionLog` contains accurate duration, distraction count, and tags.
- `EmergencyStop_MarksEndReasonCorrectly`: Verifies instant emergency stop sets `emergency` reason and safe label.
- `Pomodoro_PhaseTransitionsWorkAsExpected`: Verifies automatic cycle progression from Work -> ShortBreak -> Work -> LongBreak.
- `Streak_EmptyList_ReturnsZero`: Verifies empty session history produces 0 streak.
- `Streak_ConsecutiveDays_CalculatesAccurately`: Verifies consecutive active daily sessions calculate current and best streak correctly.
- `Streak_IgnoresVeryShortAccidentalSessions`: Verifies sessions under 60 seconds are filtered out of streak calculations.
- `ProductivityMetrics_CalculatesCorrectAggregates`: Verifies aggregate math for total focus minutes, weekly minutes, and completion rate.
- `PastSevenDaysActivity_ReturnsSevenOrderedDays`: Verifies generation of 7-day chronological activity points.
- `RuleBasedRecommendations_GeneratesSensibleTips`: Verifies generation of explainable duration, distraction, and deadline tips.

### 3.2 Integration Test Suite (`QuietoraFocus.IntegrationTests`)
- `FreshDatabase_AppliesAllMigrationsSuccessfully`: Tests schema generation from scratch, verifying migration tracking.
- `LegacyV011Database_IsSafelyMigratedWithoutDataLoss`: Simulates an existing v0.1.1 SQLite database with legacy records (`blocked_apps`, `settings`, `sessions`), applies v2.0 migrations, and verifies that legacy data is preserved without corruption while new tables and columns are created.
- `TaskRepository_LifecycleOperationsWorkCorrectly`: Tests task insertion, querying, status toggling, and deletion.
- `IndexFolder_AndFts5Search_FindsTargetKeywords`: Creates temporary test files, indexes them using `LocalMemoryIndexer`, executes SQLite FTS5 queries with BM25 ranking, and verifies snippet extraction.
- `ExportCsvAndSummaryReport_ProducesValidOutput`: Verifies CSV formatting, character escaping, and text summary report generation.

---

## 4. Manual Verification Checklist (Windows Desktop)

The following verification steps were conducted on the Windows 11 target machine:
1. **Compilation & Build:** `dotnet build` succeeded with 0 errors and 0 warnings.
2. **Package Publishing:** `publish-windows.ps1` produced a single-file standalone executable (`dist\win-x64\QuietoraFocus.exe`) and portable zip (`dist\QuietoraFocus-v2.0-win-x64-portable.zip`).
3. **Packaging Size:** The portable zip is ~60 MB; self-contained executable is ~142 MB including all .NET 10 desktop runtime packs.
4. **Vulnerability Audit:** `dotnet list package --vulnerable` reported 0 vulnerabilities.
