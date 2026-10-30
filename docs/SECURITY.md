# Security & Threat Model

## Quietora Focus v3.0 — Cybersecurity Specification
**Parent Brand:** QuietoraLabs  
**Compliance Target:** Student, Academic & Enterprise Developer Workstations

---

## 1. Threat Model & Attack Vectors

| Vector | Risk Description | Mitigation in Quietora Focus |
| :--- | :--- | :--- |
| **Data Exfiltration** | Sensitive documents or focus habits leaked to external servers. | 100% offline-first architecture. The application contains zero network sockets, HTTP clients, or telemetry endpoints for data transmission. |
| **Malicious Documents** | Ingesting infected or crafted PDFs / markdown files during memory indexing. | `DocumentExtractor.cs` uses non-executing stream readers and regex tokenizers; no native Adobe/Foxit ActiveX components or shell hooks are loaded. |
| **Prompt Injection** | Crafting text inside documents to force unwanted automated actions. | `AiAssistantService.cs` strictly enforces the `InjectionPattern` regex barrier; the assistant cannot spawn processes or write to disk. |
| **Privilege Escalation** | Blocker services abusing Administrator rights. | Elevation is strictly optional. Process minimization and `Ctrl+W` tab closing run under standard user rights. Only hosts file DNS blocking requires Admin, and users can toggle it off entirely. |
| **System Lockout** | Users becoming locked out of critical computer utilities during an emergency. | The **Emergency Stop** button is always accessible and visible; it instantly terminates all blocker loops and unbinds hosts rules. |

---

## 2. Hosts Blocker Safety & Cleanup Protocol

When the user activates system-wide Hosts File Blocking (`HostsFileService.cs`):
1. **Permission Check**: Quietora checks `WindowsBuiltInRole.Administrator`. If not elevated, it warns the user and falls back to user-mode window/tab closing.
2. **Marker Anchors**: Rules are injected between unambiguous comments:
   ```text
   # === QUIETORA FOCUS ACTIVE BLOCK START ===
   127.0.0.1 youtube.com
   127.0.0.1 www.youtube.com
   # === QUIETORA FOCUS ACTIVE BLOCK END ===
   ```
3. **Guaranteed Cleanup**: On session stop, pause, emergency stop, app exit, or crash, the cleanup routine strips all lines between the markers and restores the user's original hosts file.
