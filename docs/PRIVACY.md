# Privacy Charter & Audit Guarantee

## Quietora Focus v3.0 — User Data Sovereignty
**Brand:** QuietoraLabs  
**Motto:** "Focus on what matters. Understand your digital habits. Find your work again."

---

## 1. The Quietora Privacy Commitments

1. **Local-Only Storage**: All data — focus sessions, active tasks, indexed file snippets, distraction events, and custom rules — is stored strictly on your local PC in `%LOCALAPPDATA%\QuietoraFocus\quietora.db`.
2. **Zero Telemetry**: Quietora Focus contains no tracking pixels, analytics beacons, telemetry pings, crash reporting services, or cloud sync.
3. **On-Demand Inspection**: The foreground window inspector only operates when a focus session is actively running. When the timer is idle or paused, the monitoring loop is dormant.
4. **Transparent Codebase**: Quietora Focus is open-source. Any user or organization can audit every line of C# code, SQL migration, and Win32 P/Invoke call.
5. **Complete Data Sovereignty**:
   - Export all session records to `.csv` format at any time.
   - Export all tasks to `.csv` format at any time.
   - Export printable human-readable productivity summaries to `.txt`.
   - Perform a **Nuclear Wipe** to permanently erase the local database and settings in one click.

---

## 2. Windows Permissions & Data Flow Audit

```text
[Windows Desktop]
       │
       ▼ (Reads active window title & process name ONLY during active session)
[WindowsForegroundMonitor]
       │
       ▼ (Checks against local SQLite blocklist)
[WebsiteMatcher & Policy Evaluator]
       │
       ├─► If Distraction: Minimize / Ctrl+W & Log event to local SQLite
       └─► If Allowed: No action taken, zero logging
```

No data is ever dispatched over Wi-Fi, Ethernet, or cellular networks.
