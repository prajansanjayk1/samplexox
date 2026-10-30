# Quietora Focus MVP — v0.1.1 fixes

Fixed reported .NET 10 build errors:

- Added `using System.IO;` to `FocusStore.cs` (resolves `Path` and `Directory` CS0103 errors).
- Upgraded `Microsoft.Data.Sqlite` from 10.0.0 to 10.0.14; NuGet lists patched SQLitePCLRaw dependencies in this version.

## Run on Windows

From the extracted `QuietoraFocus-MVP` directory:

```powershell
dotnet restore .\src\QuietoraFocus\QuietoraFocus.csproj
dotnet run --project .\src\QuietoraFocus\QuietoraFocus.csproj
```

If you already extracted an older copy, prefer this new directory to avoid mixing old and new files.

The source was patched and checked for expected changes and valid project XML, but was not compiled or runtime-tested here because a Windows .NET environment is not available.
