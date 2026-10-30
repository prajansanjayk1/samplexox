# Quietora Focus v4.0 — Student OS Edition

<div align="center">

![QuietoraLabs Banner](assets/banner.png)

**"Your work, your focus, your memory — intelligently connected."**

*A comprehensive student productivity ecosystem, distraction shield, game restriction engine, safe browsing environment, and collaborative study platform built for Windows desktop.*

[![Build & Test Status](https://img.shields.io/badge/tests-72%2F72%20passing-brightgreen)](#)
[![Target Platform](https://img.shields.io/badge/platform-Windows%2010%20%7C%2011%20(x64)-blue)](#)
[![Browser Support](https://img.shields.io/badge/browser-Manifest%20V3%20Chromium-success)](#)
[![Runtime](https://img.shields.io/badge/framework-.NET%2010%20%7C%20WPF-purple)](#)
[![Offline Privacy](https://img.shields.io/badge/privacy-100%25%20Offline%20%7C%20Zero%20Cloud-success)](#)
[![Cost](https://img.shields.io/badge/infrastructure-₹0%20Budget-orange)](#)

</div>

---

## 🌟 Executive Summary

**Quietora Focus v4.0 Student OS Edition** transforms Windows into an advanced, distraction-resistant academic workstation for college students, university scholars, developers, and researchers:

1. **Browser Shield Manifest V3 Extension (`extensions/chromium/`)**: Native declarativeNetRequest blocking in Chrome, Edge, and Brave. Intercepts distracting sites and short-form video paths (`/shorts*`, `/reels*`, `/tiktok*`) at the network layer in normal and incognito windows before requests leave the PC. Dynamically synchronizes policy via local loopback on `http://127.0.0.1:41789/api/policy`.
2. **Game Restriction Engine (Game Shield)**: Auto-discovers games across Steam libraries (`steamapps\common`), Riot Games, and Epic Games with 3 configurable enforcement levels (Level 1: Detection Reminder, Level 2: Discouragement & Auto-Minimize, Level 3: Strict Focus Block) and transparent emergency escape mechanisms.
3. **Safe Browsing & Family-Safe DNS**: Category-based explicit adult and gambling domain protection combined with one-click copyable PowerShell commands to configure Cloudflare Family (1.1.1.3), AdGuard Family, and CleanBrowsing.
4. **Quietora Together (Real-Time Collaborative Study Rooms)**: Live study spaces powered by ASP.NET Core SignalR with authoritative shared countdown timers, 6-character room codes, and zero-leakage privacy.
5. **Student Academic Workspace**: University course syllabus manager, assignment deadline tracker with countdown badges, upcoming exam calendar, and interactive SuperMemo SM-2 spaced repetition flashcards.
6. **Grounded Study AI Companion**: Verified offline study summaries, 3-question conceptual practice exam quizzes, and formula cards generated on-device with zero cloud API keys.
7. **Regain Digital Detox System**: Daily app & web quotas with automated lock enforcement upon daily limit exhaustion and midnight reset.
8. **Mindful Friction Breathing Interceptor**: Calming 5-second breathing orb overlay with intentional prompts before opening distracting media.
9. **Procedural Focus Audio Generator**: 100% on-device synthesized Brown Noise, Gentle Rain, White Noise, 40Hz Gamma Binaural Beats, and 10Hz Alpha Waves.
10. **Personal PC Memory Engine (FTS5)**: Fast full-text search across local files, codebases, and notes with zero cloud exposure.
7. **Dopamine Detox Metrics**: Live Digital Detox Score (0-100), Dopamine Time Saved calculator, and doomscroll attempts thwarted tally.
8. **Multi-Mode Focus Studio & Hell Mode**: Deep Focus, Pomodoro progression, Study Mode, and Work Mode with real-time browser tab closure (`Ctrl+W`) and window minimization across Chrome, Edge, Firefox, and Brave — **including Incognito and InPrivate sessions**.
9. **Personal PC Memory Engine 2.0**: Zero-cloud SQLite FTS5 search and indexing across approved folders. Supports multi-format extraction (`.md`, `.txt`, `.pdf`, `.py`, `.cs`, `.json`, `.csv`) with content hashing and provenance tracking.
10. **Local Grounded AI Workspace Assistant**: 100% on-device AI grounded in local memory, focus logs, and tasks with citation tracking and prompt-injection defense.
11. **Contextual Work Recovery ("Resume My Work")**: Snapshot project directories, open documents, and pending tasks to restore interrupted work seamlessly.
12. **Smart Task & Focus Automations**: Trigger-condition-action rule engine for scheduled focus sessions, break reminders, and automated distraction safeguards.
13. **Global Command Palette (`Ctrl+K`)**: Instant keyboard navigation, search, and action execution across the entire workspace.
14. **Zero-Cloud Privacy Guarantee**: All data resides in a local SQLite database (`quietora.db`). No telemetry, analytics pings, or compulsory cloud accounts.

---

## 📐 System Architecture

Quietora Focus is organized into clean, decoupled layers adhering to MVVM and clean architecture principles:

```text
QuietoraFocus/
├── src/
│   ├── QuietoraFocus/               # WPF Presentation Layer (Views, ViewModels, System Tray, Interop)
│   ├── QuietoraFocus.Core/          # Pure Domain Entities, State Machine, AI RAG, Reranking, Automations
│   └── QuietoraFocus.Infrastructure/# SQLite Persistence, FTS5 Indexing, Document Extractors, Hosts Blocker
├── tests/
│   ├── QuietoraFocus.UnitTests/     # 53 Unit Tests (State Machine, AI Defense, Semantic Ranker, Extractor, Audio Synthesis, Detox Math)
│   └── QuietoraFocus.IntegrationTests/ # 11 Integration Tests (Migrations 1-5, Repositories, Budget Locks, FTS5, Exports)
├── scripts/
│   ├── publish-windows.ps1          # Self-contained single-file publish script
│   └── run-windows.ps1              # Local run script
├── dist/                            # Generated binary and portable ZIP
└── docs/                            # 12 Comprehensive Engineering Specifications
```

---

## 🚀 Quick Start & Installation

### Option 1: Portable Standalone Binary (No Install Required)
1. Download `QuietoraFocus-v3.0-win-x64-portable.zip` from `dist/`.
2. Extract the archive to any folder on your Windows PC.
3. Run `QuietoraFocus.exe`. All data is stored locally in `%LOCALAPPDATA%\QuietoraFocus\`.

### Option 2: Build from Source (.NET 10 SDK)
```powershell
# Clone the repository
git clone https://github.com/QuietoraLabs/QuietoraFocus.git
cd QuietoraFocus

# Build and execute test suite (54 tests)
dotnet build .\QuietoraFocus.sln
dotnet test .\QuietoraFocus.sln

# Launch application
dotnet run --project .\src\QuietoraFocus\QuietoraFocus.csproj
```

### Option 3: Generate Self-Contained Executable
```powershell
powershell -ExecutionPolicy Bypass -File .\scripts\publish-windows.ps1
```

---

## 🛡️ Privacy & Security Charter

| Principle | Quietora Focus Commitment |
| :--- | :--- |
| **Storage** | 100% on-device SQLite database (`quietora.db`). |
| **Network** | 0 outbound connections. No telemetry, Google Analytics, or third-party SDKs. |
| **Inspection** | Foreground window checking runs ONLY during active focus sessions. |
| **Control** | Emergency stop button instantly restores all windows and network rules. |
| **Data Freedom** | Export all history to CSV / TXT; 1-click Nuclear Wipe permanently clears all records. |

---

## 📚 Complete Documentation Suite

For detailed technical references, please consult the `docs/` directory:
- [Architecture Blueprint](docs/ARCHITECTURE.md)
- [Database & Schema Migrations](docs/DATABASE.md)
- [PC Memory Engine 2.0](docs/MEMORY_ENGINE.md)
- [Local Grounded AI Assistant](docs/LOCAL_AI.md)
- [Security & Prompt Defense](docs/SECURITY.md)
- [Privacy Charter & Audit](docs/PRIVACY.md)
- [Performance & Benchmarks](docs/PERFORMANCE.md)
- [Installation & Deployment](docs/INSTALLATION.md)
- [Testing & Quality Assurance](docs/TESTING.md)
- [Product Roadmap (v3.1 — v4.0)](docs/ROADMAP.md)
- [Release Changelog](CHANGELOG.md)

---

## ⚖️ License & Parent Brand

Copyright © 2026 **QuietoraLabs**. All rights reserved.  
Open-source under the MIT License.
