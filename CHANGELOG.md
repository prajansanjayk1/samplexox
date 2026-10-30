# Changelog

All notable changes to **Quietora Focus** are documented in this file.

---

## [4.0.0] - 2026-10-29 — Student OS Edition

### Added
- **Browser-Native Extension Shield (Manifest V3)**:
  - Manifest V3 Chromium extension for Chrome, Edge, and Brave located in `extensions/chromium`.
  - Uses `declarativeNetRequest` dynamic rules that block before network request dispatch; immune to window title polling lag.
  - Automatically redirects blocked websites and vertical feeds (`/shorts*`, `/reels*`, `/tiktok*`) to an offline dark-mode breathing page (`blocked.html`).
  - Native educational allowlist prioritizing `mit.edu`, `canvas.instructure.com`, `coursera.org`, `github.com`, `stackoverflow.com`, and `arxiv.org`.
  - Zero-dependency loopback synchronization server on `http://127.0.0.1:41789/api/policy`.
- **Game Restriction Engine (Game Shield)**:
  - Automated game discovery across Steam libraries (`steamapps\common`), Riot Games, Epic Games, and custom executables.
  - Three user-selectable enforcement tiers: Level 1 (Detection Reminder), Level 2 (Discouragement & Auto-Minimize), Level 3 (Strict Focus Block).
  - Clean emergency escape mechanisms with zero risk of irreversible Windows damage.
- **Safe Browsing & Family-Safe DNS Center**:
  - Category-based explicit adult and gambling portal filtering.
  - One-click copyable PowerShell scripts to configure Cloudflare Family (1.1.1.3), AdGuard Family, and CleanBrowsing.
  - Explicit and transparent limitations disclosure ensuring user trust.
- **Quietora Together (Real-Time Study Rooms)**:
  - ASP.NET Core SignalR collaboration backend (`QuietoraFocus.Realtime`) in `server/QuietoraFocus.Realtime`.
  - Authoritative synchronized session timers across study group members.
  - Instant room creation with 6-character invitation codes (e.g., `DSA901`).
  - Zero-leakage privacy: no process titles, keystrokes, or screen contents transmitted.
- **Student Academic Workspace**:
  - Subject and syllabus planner with credit hours and topic completion bars.
  - Assignment tracker with days-remaining countdown badges.
  - Exam calendar countdown cards.
  - SuperMemo SM-2 Spaced Repetition engine with mathematical ease factor adjustments and interval growth.
  - Grounded Study AI companion generating deterministic, verified summaries, exam quizzes, and formula cards without cloud dependencies.
- **Database Schema Migration 6**:
  - Added `academic_subjects`, `academic_topics`, `academic_assignments`, `academic_exams`, `academic_flashcards`, and `detected_games` with pre-seeded STEM curricula.
- **Automated Verification**:
  - Expanded test suite to **72 automated tests** (59 unit tests, 13 integration tests), achieving 100% pass rate.

---

## [3.1.0] - 2026-10-29 — Regain Detox & Intelligence Edition

### Added
- **Regain Digital Detox System for Windows**:
  - Daily App & Website screen-time quotas/budgets (YouTube 30m, Reddit 20m, Instagram 15m, Discord 45m, Steam 30m).
  - Real-time active window foreground tracking in 1-second ticks, automatically locking applications upon daily quota exhaustion.
  - Automatic midnight reset of daily usage timers.
- **Targeted YouTube Shorts & Instagram Reels Shield**:
  - Surgical doomscroll feed detection (`/shorts`, `Reels`, `TikTok`, `r/popular`) that blocks vertical short-form addiction in < 1 second while leaving educational long-form videos and research uninterrupted.
- **Mindful Friction & The Breathing Interceptor**:
  - Calming 5-second breathing orb overlay with gentle color transitions and intention prompts ("Take a breath", "Stay focused", or take a timed 2-minute break) before unlocking restricted media.
- **Bedtime & Circadian Shield**:
  - Automated evening lock (22:30 to 07:00) that protects deep sleep cycles and halts late-night blue-light binge sessions.
- **Procedural Focus Soundscape Synthesizer**:
  - 100% on-device procedural 16-bit PCM stereo WAV audio generator.
  - Zero external MP3 downloads or network dependencies.
  - Produces continuous Brown Noise (ADHD deep rumble), Gentle Rain (LFO-modulated drops), White Noise, 40Hz Gamma Binaural Beats (coding & intense concentration), and 10Hz Alpha Waves (relaxed study).
- **Virtual Co-Working Lounge**:
  - Simulated live quiet study lounge with peer companion avatars, active countdowns, study goals, and live focus streaks.
- **Dopamine Detox Metrics**:
  - Digital Detox Score (0-100), Dopamine Time Saved calculator (8.5m saved per averted doomscroll session), and live counter of thwarted distraction attempts.
- **Database Migration 5**:
  - Added `app_budgets` and `screen_time_records` tables with pre-seeded budgets.
- **Automated Verification**:
  - Expanded test suite to **64 passing automated tests** (53 unit tests, 11 integration tests, 0 failures).

---

## [3.0.0] - 2026-10-29 — Intelligence Edition

### Added
- **Local Grounded AI Workspace Assistant**:
  - 100% on-device AI grounded in indexed documents, focus logs, and tasks with auditable source citations.
  - Three operating modes: *Lite (Fast CPU RAG)*, *Standard (Hybrid BM25 + TF-IDF Vector Rerank)*, and *Deep Analysis*.
  - Prompt-injection defense engine intercepting system command overrides and malicious script directives.
- **Contextual Work Recovery ("Resume My Work")**:
  - Directory snapshot capture recording open files, associated tasks, and notes.
  - One-click workspace restoration opening projects directly in Windows File Explorer.
  - Suggested next action synthesis for pending tasks and interrupted work.
- **Smart Task & Focus Automations**:
  - Trigger-condition-action rule engine supporting scheduled times, session completions, and distraction thresholds.
  - Pre-seeded automation rules for morning deep focus blocks, post-session rest breaks, and distraction spike alerts.
- **Global Command Palette (`Ctrl+K`)**:
  - Modal overlay for rapid keyboard navigation across all 10 workspaces, quick session starts, and actions.
- **Personal PC Memory Engine 2.0**:
  - Native multi-format extraction for Markdown, text, code, structured formats, and PDF streams.
  - SHA-256 content deduplication and provenance tracking.
  - SQLite FTS5 Porter-stemming full-text search with context snippet highlighting.
- **Hell / Hardcore Mode & Website Shield**:
  - Real-time tab closing (`Ctrl+W`) and window minimization within 1 second.
  - InPrivate and Incognito browser window title detection for YouTube, Instagram, Reddit, TikTok, Netflix, and custom sites across Chrome, Edge, Firefox, and Brave.
  - Optional system-wide Hosts file DNS blocker with clean marker removal.
- **Database Migration 4**:
  - Added `workspace_snapshots` and `automation_rules` tables with transaction rollback safety.
- **Automated Verification**:
  - Expanded test suite to **54 passing automated tests** (45 unit tests, 9 integration tests, 0 failures).
- **12 Comprehensive Technical Specifications**:
  - Full engineering documentation added to `docs/`.

---

## [2.1.0] - 2026-10-29 — Hardcore Distraction Shield

### Added
- Browser window title inspector for foreground process monitoring.
- Tab-close intervention via native Win32 keyboard event injection (`Ctrl+W`).
- `blocked_websites` table with pre-seeded entertainment and social media domains.
- System-wide Hosts file blocking service with administrator role elevation checks.

---

## [2.0.0] - 2026-10-29 — Advanced Edition

### Added
- Clean modular 3-project solution architecture (`QuietoraFocus.Core`, `QuietoraFocus.Infrastructure`, `QuietoraFocus`).
- Monotonic state machine for focus sessions supporting Deep Focus, Pomodoro, Study Mode, and Work Mode.
- Task management system with priorities and duration estimation.
- SQLite FTS5 Personal PC Memory indexing across user-approved folders.
- Win32 native system tray integration (`Shell_NotifyIconW`).
- Dynamic Fluent Dark/Light Theme Manager.
- CSV and summary text report export services.

---

## [0.1.1] - 2026-10-28 — MVP Baseline Fixes

### Fixed
- Resolved missing `System.IO` imports in `FocusStore.cs`.
- Upgraded `Microsoft.Data.Sqlite` to 10.0.14 addressing previous package warnings.
- Fixed letter-spacing and namespace ambiguities in initial WPF project.
