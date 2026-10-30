# Quietora Focus v4.0 — Student OS Edition
## Comprehensive Student Workspace, Distraction Shield & Study Platform Manual

**Parent Brand:** QuietoraLabs  
**Product:** Quietora Focus v4.0 — Student OS Edition  
**Platform:** Windows 10 & 11 (x64) • Chromium Browsers (Google Chrome, Microsoft Edge, Brave)  
**Infrastructure Cost:** ₹0 (Zero mandatory cloud services; 100% offline-ready desktop client)

---

## 1. System Architecture Overview

Quietora Focus v4.0 is engineered specifically for college students, university scholars, developers, and competitive exam aspirants. It organizes digital productivity into five tightly integrated layers:

```
+-------------------------------------------------------------------------+
|                  Quietora Focus v4.0 — Student OS                        |
+-------------------------------------------------------------------------+
| Layer A: Desktop Client (C# .NET 10 WPF + Fluent Dark Ergonomics)       |
| Layer B: Game Restriction Engine (Steam, Riot, Epic, Standalone Scanner)|
| Layer C: Browser Shield (Manifest V3 DNR + Local HTTP Loopback Sync)    |
| Layer D: Quietora Together (SignalR Authoritative Real-Time Study Rooms)|
| Layer E: Academic Workspace (Syllabus, Assignments, SM-2 Flashcards)   |
+-------------------------------------------------------------------------+
| Local Persistence: SQLite WAL Mode • FTS5 Full-Text Engine • 6 Migrations|
+-------------------------------------------------------------------------+
```

---

## 2. Browser Shield Installation & Setup (Manifest V3)

Quietora Focus replaces fragile window-polling with native **Manifest V3 `declarativeNetRequest` rules**, guaranteeing that blocked domains, incognito tabs, and short-form video paths (`/shorts*`, `/reels*`) are blocked **before** network requests leave the browser.

### Loading Unpacked in Chrome / Edge / Brave:

1. Open your browser and navigate to the extensions page:
   - **Google Chrome:** `chrome://extensions`
   - **Microsoft Edge:** `edge://extensions`
   - **Brave:** `brave://extensions`
2. Enable **Developer mode** (toggle in top right corner).
3. Click **Load unpacked** (or "Load unpacked extension").
4. Select the folder:
   `extensions/chromium` (or `dist/win-x64/browser-extension` in distribution packages).
5. The **Quietora Browser Shield** icon will appear in your browser toolbar.
6. The extension communicates with Quietora Focus desktop via local HTTP loopback:
   `http://127.0.0.1:41789/api/policy`
   - When a focus session begins or adult shield is enabled, rules update dynamically in milliseconds.
   - Blocked domains and shorts are redirected to a calm, offline dark-mode page (`blocked.html`).

---

## 3. Game Restriction Engine (Game Shield)

Game Shield actively prevents impulsive gaming during scheduled study blocks.

### Discovery Engine
- **Steam Auto-Discovery:** Scans `steamapps\common` across default drives and registry keys (`Counter-Strike 2`, `Dota 2`, `Grand Theft Auto V`, `Apex Legends`, etc.).
- **Riot Games:** Scans Riot Games install roots (`Valorant`, `League of Legends`).
- **Epic Games & Standalone:** Detects Unreal Engine and Epic library manifests.
- **Custom Executables:** Students can add any custom game or emulator by process name (e.g., `yuzu`, `rpcs3`, `genshin`).

### Enforcement Levels
1. **Level 1 — Detection Reminder:** Gently displays an awareness notification alert when a game launches outside of an active focus session.
2. **Level 2 — Discouragement & Auto-Minimize:** Automatically minimizes the game window and presents an encouraging notification to return to study.
3. **Level 3 — Strict Focus Block:** Instantly minimizes the game and refocuses Quietora Focus / study material. In active focus sessions, this records a distraction event.
- **Emergency Recovery:** Pressing the global Emergency Stop button or `Ctrl+K` immediately restores full controls and releases restrictions.

---

## 4. Safe Browsing & Family DNS Center

Protects students against explicit adult content, gambling, and malicious ads.

### A. Extension-Level Protection
- Priority 2500 `declarativeNetRequest` dynamic rules intercept explicit adult portals, adult streaming networks, and gambling domains.
- Priority 3000 rules ensure educational portals (`mit.edu`, `canvas.instructure.com`, `coursera.org`, `github.com`, `stackoverflow.com`, `arxiv.org`) can never be inadvertently blocked.

### B. System-Wide Family-Safe DNS Resolvers
Quietora provides one-click PowerShell scripts to bind the primary network interface (e.g., `Wi-Fi` or `Ethernet`) to family-safe upstream DNS:
- **Cloudflare 1.1.1.3:** Fast Anycast resolution with automated malware and adult domain blocking (`1.1.1.3` / `1.0.0.3`).
- **AdGuard Family:** Explicit domain filtering, ad blocking, and search-engine SafeSearch enforcement (`94.140.14.15` / `94.140.15.16`).
- **CleanBrowsing:** Strict household/school filter (`185.228.168.168`).

---

## 5. Quietora Together — Collaborative Study Rooms

Allows study groups to co-work with shared accountability.

- **Authoritative Shared Timer:** The host sets the target duration (e.g. 45 min). All connected peers count down simultaneously via the SignalR server (`QuietoraFocus.Realtime`).
- **6-Character Room Codes:** Easy peer-to-peer invitation codes (e.g., `DSA901`, `BIO402`).
- **Zero-Leakage Privacy:** No personal screen captures, keystrokes, or active window titles are ever broadcast to peers. Only avatar, study task, readiness status, and chat messages are shared.

---

## 6. Academic Workspace & Spaced Repetition (SuperMemo SM-2)

### Academic Syllabus & Subjects
- Track semester courses with credit hours and target study commitments.
- Syllabus chapters with estimated completion times and topic checkmarks.

### Assignment Tracker
- Manage problem sets, essays, and lab submissions.
- Real-time days-remaining badges that highlight approaching deadlines.

### SuperMemo SM-2 Spaced Repetition Engine
Mathematically schedules review intervals based on human memory decay curves:
- **Ease Factor Formula:**  
  $$EF' = EF + (0.1 - (5 - q) \times (0.08 + (5 - q) \times 0.02))$$  
  *(with $EF' \ge 1.3$ constraint)*
- **Interval Growth:**
  - $I(1) = 1 \text{ day}$
  - $I(2) = 6 \text{ days}$
  - $I(n) = \lceil I(n-1) \times EF' \rceil$
- **Rating Scales:**  
  1 = Blackout, 2 = Incorrect, 3 = Difficult, 4 = Good Recall, 5 = Perfect Recall.

### Grounded Study AI Companion
Generates verified academic summaries, 3-question conceptual practice exams, and formula review cards without cloud API dependencies.
