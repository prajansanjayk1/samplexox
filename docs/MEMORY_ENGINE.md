# Personal PC Memory Engine 2.0

## Quietora Focus v3.0 — On-Device Memory Specification
**Subsystem:** `QuietoraFocus.Infrastructure.Indexing` & `QuietoraFocus.Core.Services`

---

## 1. Engine Overview

The **Personal PC Memory Engine** enables students, developers, and researchers to index and search their locally stored knowledge bases, codebases, and literature with zero reliance on cloud vector databases or subscriptions.

Key design tenants:
- **Strict User Consent**: Only directories explicitly approved by the user via the Memory Manager are scanned.
- **Privacy First**: Raw file contents are converted into searchable indexes on-device; no data is ever uploaded.
- **Incremental Crawling**: File modification timestamps and SHA-256 hashes prevent re-indexing unchanged files.

---

## 2. Multi-Format Extraction Pipeline

`DocumentExtractor.cs` inspects target files and routes them to specialized content decoders:

| Extension Category | Formats | Processing Method |
| :--- | :--- | :--- |
| **Markdown & Notes** | `.md`, `.markdown`, `.txt`, `.rtf` | Fast UTF-8 streaming reader with memory buffering (up to 64KB per file). |
| **Source Code** | `.cs`, `.py`, `.js`, `.ts`, `.html`, `.css`, `.cpp`, `.c`, `.java`, `.go`, `.rs`, `.sql`, `.sh` | Code tokenizer extracting comments, function names, and structural definitions. |
| **Data & Configuration** | `.json`, `.csv`, `.xml`, `.yaml`, `.yml`, `.toml`, `.ini` | Structured token extraction prioritizing key-value identifiers and labels. |
| **PDF Documents** | `.pdf` | Native regex stream extractor parsing text blocks (`(text) Tj` and `[tokens] TJ`) without heavyweight external native C++ dependencies. |

---

## 3. SQLite FTS5 Full-Text Index

Indexed content is stored in the `memory_fts` virtual table powered by SQLite's FTS5 module:

```sql
CREATE VIRTUAL TABLE IF NOT EXISTS memory_fts USING fts5(
    file_id UNINDEXED,
    file_name,
    content,
    tokenize = 'porter unicode61'
);
```

### Search Features:
- **Porter Stemming**: Automatically matches inflected word variants (e.g. "indexing", "indexed", "indexes").
- **Unicode Support**: Seamlessly searches across multi-lingual documents.
- **Snippet Highlighting**: Returns extracted context excerpts using `snippet(memory_fts, 2, '<b>', '</b>', '...', 32)`.
