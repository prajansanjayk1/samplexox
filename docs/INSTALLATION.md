# Installation & Deployment Guide

## Quietora Focus v3.0 — Setup Instructions
**Supported Operating Systems:** Windows 10 (Build 19041+) and Windows 11 (all editions)  
**Architecture:** x64

---

## 1. Quick Setup (Portable Edition)

The portable edition requires zero administrative rights and does not modify the Windows registry:

1. Download `QuietoraFocus-v3.0-win-x64-portable.zip` from the release assets or `dist/` directory.
2. Unzip into a convenient directory (e.g. `C:\Tools\QuietoraFocus\` or `%USERPROFILE%\AppData\Local\Programs\QuietoraFocus\`).
3. Double-click `QuietoraFocus.exe` to launch.
4. Pin the application to your Windows Taskbar or Start Menu for quick access.

---

## 2. Developer & Building from Source

### Prerequisites:
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 10/11 x64
- PowerShell 7+ or Windows PowerShell 5.1

### Compilation Steps:
```powershell
# 1. Restore dependencies
dotnet restore .\QuietoraFocus.sln

# 2. Compile Debug build
dotnet build .\QuietoraFocus.sln

# 3. Execute all unit and integration tests (54 tests)
dotnet test .\QuietoraFocus.sln --configuration Release

# 4. Generate Single-File Self-Contained Release Binary
powershell -ExecutionPolicy Bypass -File .\scripts\publish-windows.ps1
```

The compiled executable will be placed in `dist\win-x64\QuietoraFocus.exe` alongside the generated portable archive `dist\QuietoraFocus-v3.0-win-x64-portable.zip`.

---

## 3. Configuration & Database Location

- **Database Path**: `%LOCALAPPDATA%\QuietoraFocus\quietora.db`
- **Application Logs**: `%LOCALAPPDATA%\QuietoraFocus\logs\app.log`
- **Backup Recommendation**: Copy `quietora.db` to back up all session records, task lists, and configuration rules.
