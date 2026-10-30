# Quietora Focus v2.0 — Setup & Developer Guide

## Prerequisites

To build and run Quietora Focus from source, you need:

1. **Operating System:** Windows 10 (1809+) or Windows 11 (x64).
2. **.NET SDK:** Microsoft .NET 10 SDK (version 10.0.100 or higher). Verify by opening PowerShell and executing:
   ```powershell
   dotnet --version
   ```
3. **Optional IDE:** Visual Studio 2026 / Visual Studio Code with the C# Dev Kit extension.

---

## 🛠️ Step-by-Step Setup

### 1. Clone or Extract the Source
Extract the repository folder to your local drive (e.g. `C:\Users\Username\Projects\QuietoraFocus-MVP`).

### 2. Restore Dependencies
In PowerShell, navigate to the project directory and restore NuGet packages:
```powershell
dotnet restore .\QuietoraFocus.sln
```

### 3. Build the Entire Solution
Compile the Core, Infrastructure, Presentation, and Test projects:
```powershell
dotnet build .\QuietoraFocus.sln --configuration Release
```

### 4. Execute the Automated Test Suite
Run both Unit and Integration test projects:
```powershell
dotnet test .\QuietoraFocus.sln --configuration Release
```

### 5. Launch the Application
Start the desktop application:
```powershell
.\scripts\run-windows.ps1
```
Or directly via the .NET CLI:
```powershell
dotnet run --project .\src\QuietoraFocus\QuietoraFocus.csproj
```

---

## 📦 Packaging for Distribution

To create a single-file, self-contained standalone executable that can run on any Windows 10/11 x64 PC without requiring an existing .NET runtime installation:

```powershell
.\scripts\publish-windows.ps1
```

This script produces:
- Single-file binary: `dist\win-x64\QuietoraFocus.exe`
- Portable distribution archive: `dist\QuietoraFocus-v2.0-win-x64-portable.zip`

---

## 🔧 Database & Storage Locations

All application state is saved locally under the standard Windows LocalAppData directory:
- **Database:** `%LOCALAPPDATA%\QuietoraLabs\QuietoraFocus\focus.db`
- **Diagnostic Logs:** `%LOCALAPPDATA%\QuietoraLabs\QuietoraFocus\logs\quietora_YYYYMMDD.log`

To inspect the SQLite database using SQLite CLI:
```powershell
sqlite3 "$env:LOCALAPPDATA\QuietoraLabs\QuietoraFocus\focus.db" ".tables"
```

To reset the database manually, delete the folder `%LOCALAPPDATA%\QuietoraLabs\QuietoraFocus` or use the **Nuclear Wipe** button inside the in-app Privacy Center.
