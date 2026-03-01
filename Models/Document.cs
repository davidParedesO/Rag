using System;
using System.Text.Json.Serialization;

namespace Rag.Models;

public class Document
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    [JsonPropertyName("docpath")]
    public string DocPath { get; set; } = string.Empty;

    public string DisplayName => !string.IsNullOrEmpty(Title) ? Title : Name;
}

public class DocumentResponse
{
    public List<Document> Documents { get; set; } = new();
}

// ─── Upload response ────────────────────────────────────────────────────────

public class UploadDocumentResponse
{
    public bool Success { get; set; }
    public List<UploadedDocument>? Documents { get; set; }
}

public class UploadedDocument
{
    public string? Location { get; set; }
    public string? Name { get; set; }
    [JsonPropertyName("docpath")]
    public string? DocPath { get; set; }
}

// ─── GET /v1/documents response (árbol de carpetas) ────────────────────────

public class GlobalDocumentsResponse
{
    public LocalFilesRoot? LocalFiles { get; set; }
}

public class LocalFilesRoot
{
    public List<FolderItem> Items { get; set; } = new();
}

public class FolderItem
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;   // "folder" | "file"
    public List<FolderItem> Items { get; set; } = new();

    // Campos de archivo
    public string? Id { get; set; }
    public string? Title { get; set; }
    public string? Location { get; set; }
    [JsonPropertyName("docpath")]
    public string? DocPath { get; set; }
    public DateTime? CreatedAt { get; set; }
}
