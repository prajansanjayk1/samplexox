# Quietora Focus — Privacy Charter

## Our Core Commitment
Quietora Focus is built on a simple philosophy: **Your computer, your data, your privacy.**

Quietora Focus does not require an account, does not connect to cloud servers, does not log keystrokes, and does not record your screen.

---

## 1. What Data Is Stored Locally

All data is stored exclusively on your device in `%LOCALAPPDATA%\QuietoraLabs\QuietoraFocus\focus.db`:

- **Focus Sessions:** Task title, subject tag, project name, UTC timestamps, planned duration, actual duration, paused duration, distraction counts, and session notes.
- **Tasks & Goals:** Task titles, priorities, subject tags, estimated minutes, status (pending/completed).
- **Distraction Rules:** User-entered process names (e.g. `discord`, `steam`).
- **Distraction Event Logs:** Timestamp, process name, and policy applied (e.g. "Reminded", "Minimized").
- **PC Memory Index:** File paths, file names, file sizes, last modified timestamps, and text snippets of documents within folders that you explicitly chose to index.
- **Settings:** Preferences such as theme (dark/light), startup behavior, and default session duration.

---

## 2. What Is NEVER Collected

- ❌ **No Cloud Sync or Analytics:** No telemetry, tracking pixels, or external API calls.
- ❌ **No Screen Captures:** No screenshots, screen recordings, or webcam access.
- ❌ **No Keystroke Logging:** No recording of typing, clipboard, or microphone inputs.
- ❌ **No Web Browsing History:** Does not read browser URLs, cookies, or history.
- ❌ **No Inactive Monitoring:** Process checks run **only** when a focus session is actively running. When idle or closed, monitoring is completely halted.

---

## 3. Data Ownership & Deletion

- **Export Your Data:** You can export all session records and tasks to open standard CSV files at any time via the Analytics or Privacy Center pages.
- **Nuclear Wipe:** The Privacy Center provides a one-click "Delete All Local Data" button that wipes all sessions, tasks, memory indexes, and settings from your device.
- **Manual Removal:** Deleting the folder `%LOCALAPPDATA%\QuietoraLabs\QuietoraFocus` completely erases all trace of the application's data.
