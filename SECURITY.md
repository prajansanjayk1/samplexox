# Quietora Focus — Security & Safety Model

## 1. Security Architecture

Quietora Focus is designed around a **Zero Trust for External Networks** model. The software has no network sockets, no cloud telemetry, and no remote dependencies.

### Key Security Postures:
1. **Parameterized Queries:** All SQLite interactions use parameterized variables (`$name`, `$task`, `$val`) via `Microsoft.Data.Sqlite`, preventing SQL injection vulnerabilities.
2. **Standard User Privileges:** Quietora Focus never requires elevated (Administrator) permissions.
3. **Protected Process Whitelist:** Prevents users from accidentally adding critical Windows processes to distraction lists:
   - `explorer`, `taskmgr`, `dwm`, `winlogon`, `csrss`, `lsass`, `services`, `system`, `svchost`, `smss`, `wininit`, `sihost`, `msmpeng`, `securityhealthsystray`, `searchhost`, `startmenuexperiencehost`, `cmd`, `powershell`.
4. **Input Sanitization:** Process names are sanitized via regex (`^[a-z0-9._-]{1,60}$`), stripped of `.exe` suffixes, and normalized to lowercase.
5. **Bounded File Reading:** Local PC Memory indexing checks file sizes before reading (max 5 MB) and reads at most 64 KB of text per file, preventing out-of-memory denial-of-service conditions.
6. **Diagnostic Log Scrubbing:** The logging subsystem strictly writes operational events (such as session start, schema migration, or indexing counts) and never logs keystrokes, window titles, URLs, or file contents.

---

## 2. Threat Modeling & Limitations

| Potential Risk | Mitigation in Quietora Focus |
|---|---|
| Malformed or locked files during indexing | `LocalMemoryIndexer` catches IO exceptions, ignores system/hidden files, and skips unreadable files gracefully. |
| Database lock contention | SQLite configured with Write-Ahead Logging (`PRAGMA journal_mode=WAL;`), foreign keys enabled, and busy timeouts. |
| Window focus theft | Distraction Level 2 (Remind) presents a non-intrusive banner rather than forcing window activation. |
| Accidental data loss during upgrade | Schema migrations are executed inside SQLite transactions with version verification. Existing databases are never overwritten. |
| Accidental destructive deletion | The "Nuclear Wipe" feature requires explicit multi-step confirmation before dropping data. |

---

## 3. Vulnerability Reporting

If you discover a security defect or vulnerability, please open an issue in the QuietoraLabs repository or reach out directly to the maintainers. As Quietora Focus is an offline application, local file security relies on the standard Windows User Profile encryption and access controls (BitLocker / NTFS ACLs).
