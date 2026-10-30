# Quietora Focus v2.0 — Feature Catalog

## 1. Multi-Mode Focus Engine

| Mode | Target User | Default Duration | Behavior |
|---|---|---|---|
| **Deep Focus** | Developers, Writers, Researchers | 50 minutes | Extended uninterrupted work block with gentle background distraction monitoring. |
| **Pomodoro** | Students, Task Managers | 25 min work / 5 min short break | Automatic cycle progression. Every 4 work intervals, triggers a 15-minute long break. |
| **Study Mode** | Exam Students, Course Learners | Configurable (25-90m) | Includes Subject/Course tag and dedicated session notes saved into SQLite history. |
| **Work Mode** | Project Contributors, Freelancers | Configurable | Links sessions to specific project deliverables and active tasks. |
| **Custom Mode** | Advanced Users | User-defined | Configurable duration, intervals, and distraction sensitivity. |

---

## 2. Smart Distraction Management & Hell / Hardcore Mode

- **Foreground Window & Title Inspection:** Analyzes active window process name and browser window titles.
- **Incognito & InPrivate Window Interception:** Monitors browser tab titles across Google Chrome, Microsoft Edge, Mozilla Firefox, Brave, Opera, etc. Detects and intercepts YouTube, Instagram, Reddit, etc. regardless of whether the user is in normal browsing or private/incognito mode!
- **Enforcement Modes:**
  - **🔥 Hell / Hardcore Mode (Active by default):**
    - Instant tab termination: Sends keystroke `Ctrl+W` to terminate the active distracting tab.
    - Window minimization: Immediately minimizes the browser window.
    - 1-second rapid re-check loop: Eliminates any 15-second grace period so users cannot maximize and continue watching.
  - **Strict Mode:** Persistent window minimization with a 2-second re-check loop.
  - **Gentle Mode:** Non-intrusive reminder banner with a 15-second cooldown.
- **System-Wide Hosts File DNS Blocker (Optional):**
  - If running as Administrator, redirects target domains (`youtube.com`, `instagram.com`, `reddit.com`, etc.) directly to `127.0.0.1` in `C:\Windows\System32\drivers\etc\hosts`.
  - Automatically and cleanly restored upon session stop, completion, or emergency stop.
- **System Safeguards:** Automatically ignores and protects Windows system processes (`explorer`, `taskmgr`, `dwm`, `winlogon`, `system`, `cmd`, `powershell`, etc.). Emergency Stop button is always accessible.

---

## 3. Personal PC Memory (Local FTS5 Full-Text Search)

- **Explicit Folder Opt-in:** Only user-approved folders are indexed.
- **File Type Support:** Indexes common text and code formats (`.md`, `.txt`, `.py`, `.cs`, `.js`, `.ts`, `.json`, `.csv`, `.html`, `.sql`, etc.).
- **Safe Parsing:** Bounded to 5 MB per file and 64 KB indexed content buffer to prevent unbounded memory spikes.
- **Lightning-Fast Retrieval:** Powered by SQLite FTS5 with BM25 relevance ranking and contextual snippet highlighting.
- **Direct File Navigation:** Double-click or click "Open File" to open the local document directly with default Windows application.
- **Index Management:** View total indexed files, disk footprint, rebuild index, or wipe index completely with one click.

---

## 4. Integrated Tasks & Goals

- Create, edit, complete, and delete tasks.
- Priority classifications: **Low**, **Medium**, **High**, **Urgent**.
- Assign estimated duration, subject tags, and due dates.
- Filter tasks by active status or completed status.
- Directly select tasks to focus on when starting a session.

---

## 5. Productivity Analytics & Insights

- **7-Day Interactive Activity Distribution:** Displays daily focused minutes and distraction counts.
- **Core Metrics:** Total focus time (hours), today's focus minutes, weekly minutes, session completion rate (%), current daily streak, best streak.
- **Distraction Frequency:** Visualizes top 5 distracting processes with percentage shares.
- **Rule-Based Recommendations:**
  - Advises shorter or longer session durations based on recent completion rates.
  - Warns when distraction interventions spike and suggests policy adjustments.
  - Flags overdue tasks and upcoming deadlines.
- **Transparent Heuristics:** Clear disclosure of calculation formulas; no fabricated psychological scores.

---

## 6. Desktop Integration & Experience

- **Fluent Design System:** Windows 11-inspired aesthetics with smooth Dark and Light mode themes.
- **Native System Tray:** Minimizes to taskbar notification area; double-click restores window.
- **Startup Integration:** User-controlled toggle to launch Quietora Focus on Windows boot.
- **Sound Alerts:** Audio feedback upon session completion.
- **Data Export:** Instant export of sessions, tasks, and distraction records to standard CSV files.
- **Nuclear Wipe:** Privacy Center option to permanently delete all local application data.
