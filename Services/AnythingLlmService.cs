using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Rag.Models;

namespace Rag.Services;

public class AnythingLlmService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "#";
    private const string ApiKey = "#";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public AnythingLlmService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {ApiKey}");
    }

    // ─── Workspaces ────────────────────────────────────────────────────────────

    public async Task<List<Workspace>> GetWorkspacesAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/workspaces");
            if (!response.IsSuccessStatusCode) return new List<Workspace>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<WorkspaceResponse>(json, JsonOptions);
            return result?.Workspaces ?? new List<Workspace>();
        }
        catch (Exception ex) { Console.WriteLine($"GetWorkspacesAsync error: {ex.Message}"); return new List<Workspace>(); }
    }

    public async Task<bool> CreateWorkspaceAsync(string name)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/workspace/new", new { name });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) { Console.WriteLine($"CreateWorkspaceAsync error: {ex.Message}"); return false; }
    }

    public async Task<bool> UpdateWorkspaceAsync(string slug, string newName)
    {
        try
        {
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/workspace/{slug}/update", new { name = newName });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) { Console.WriteLine($"UpdateWorkspaceAsync error: {ex.Message}"); return false; }
    }

    /// <summary>
    /// POST /v1/workspace/{slug}/update — actualiza system prompt y modo de chat.
    /// </summary>
    public async Task<bool> UpdateWorkspaceSettingsAsync(string slug, string systemPrompt, string chatMode)
    {
        try
        {
            var body = new
            {
                openAiPrompt = systemPrompt,
                chatMode = chatMode   // "chat" o "query"
            };
            var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/workspace/{slug}/update", body);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) { Console.WriteLine($"UpdateWorkspaceSettingsAsync error: {ex.Message}"); return false; }
    }

    public async Task<bool> DeleteWorkspaceAsync(string slug)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/workspace/{slug}");
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex) { Console.WriteLine($"DeleteWorkspaceAsync error: {ex.Message}"); return false; }
    }

    // ─── Chat ──────────────────────────────────────────────────────────────────

    public async Task<ChatMessage> SendChatMessageAsync(string workspaceSlug, string message, string mode = "chat")
    {
        try
        {
            var body = new { message, mode };
            var json = JsonSerializer.Serialize(body);
            var content = new StringContent(json, Encoding.UTF8, "application/json");



            var response = await _httpClient.PostAsync($"{BaseUrl}/workspace/{workspaceSlug}/chat", content);
            if (!response.IsSuccessStatusCode)
                return new ChatMessage { Role = "assistant", Content = "Error al comunicarse con la IA." };

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AnythingLlmChatResponse>(responseJson, JsonOptions);

            return new ChatMessage
            {
                Role = "assistant",
                Content = result?.textResponse ?? "Sin respuesta.",
                Chunks = result?.sources?.Select(s => new Chunk
                {
                    Title = s.title ?? "Documento desconocido",
                    Text = s.text ?? "",
                    Score = Math.Round(s.score, 3)
                }).ToList() ?? new List<Chunk>()
            };
        }
        catch (Exception ex)
        {
            return new ChatMessage { Role = "assistant", Content = $"Error: {ex.Message}" };
        }
    }

    // ─── Documentos ────────────────────────────────────────────────────────────

    /// <summary>
    /// Sube el archivo al almacén global y luego llama a update-embeddings
    /// para vincularlo al workspace.
    /// </summary>
    public async Task<bool> UploadDocumentAsync(string workspaceSlug, FileResult fileResult)
    {
        try
        {
            // 1. Subir al almacén global
            var form = new MultipartFormDataContent();
            var fileBytes = await ReadAllBytesAsync(fileResult);
            var fileContent = new ByteArrayContent(fileBytes);
            fileContent.Headers.ContentType =
                new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
            form.Add(fileContent, "file", fileResult.FileName);

            var uploadResponse = await _httpClient.PostAsync($"{BaseUrl}/document/upload", form);
            if (!uploadResponse.IsSuccessStatusCode)
            {
                return false;
            }

            // 2. Extraer "location" de la respuesta
            var uploadJson = await uploadResponse.Content.ReadAsStringAsync();
            using var docDoc = JsonDocument.Parse(uploadJson);

            var fullPath = docDoc.RootElement
                .GetProperty("documents")[0]
                .GetProperty("location")
                .GetString();

            if (string.IsNullOrEmpty(fullPath))
            {
                return false;
            }

            // Normalizar a ruta relativa: "custom-documents/..."
            var idx = fullPath.IndexOf("custom-documents", StringComparison.OrdinalIgnoreCase);
            var location = (idx >= 0 ? fullPath[idx..] : fullPath).Replace("\\", "/");

            // 3. Vincular al workspace con update-embeddings
            return await UpdateEmbeddingsAsync(workspaceSlug, adds: new[] { location });
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// POST /v1/workspace/{slug}/update-embeddings
    /// </summary>
    public async Task<bool> UpdateEmbeddingsAsync(string workspaceSlug, string[]? adds = null, string[]? deletes = null)
    {
        try
        {
            var payload = new
            {
                adds = adds ?? Array.Empty<string>(),
                deletes = deletes ?? Array.Empty<string>()
            };


            var response = await _httpClient.PostAsJsonAsync(
                $"{BaseUrl}/workspace/{workspaceSlug}/update-embeddings", payload);

            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            return false;
        }
    }

    /// <summary>
    /// GET /v1/workspace/{slug} — obtiene los documentos del workspace.
    /// Usa JsonDocument para acceder directamente a los campos sin depender de la estructura exacta.
    /// </summary>
    public async Task<List<Document>> GetDocumentsAsync(string workspaceSlug)
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/workspace/{workspaceSlug}");
            if (!response.IsSuccessStatusCode) return new List<Document>();

            var json = await response.Content.ReadAsStringAsync();

            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("workspace", out var wsElement))
                return new List<Document>();

            // Workspace puede ser un objeto o un array
            JsonElement wsObj;
            if (wsElement.ValueKind == JsonValueKind.Array)
            {
                if (wsElement.GetArrayLength() == 0) return new List<Document>();
                wsObj = wsElement[0];
            }
            else
            {
                wsObj = wsElement;
            }

            if (!wsObj.TryGetProperty("documents", out var docsEl))
                return new List<Document>();

            var result = new List<Document>();
            foreach (var d in docsEl.EnumerateArray())
            {
                // El endpoint devuelve 'docpath' o 'uri' como ruta del documento
                var docpath = TryGetString(d, "docpath") ?? TryGetString(d, "uri") ?? string.Empty;

                result.Add(new Document
                {
                    Id = TryGetString(d, "id") ?? TryGetString(d, "docId") ?? string.Empty,
                    Name = TryGetString(d, "name") ?? Path.GetFileName(docpath),
                    Title = TryGetString(d, "title") ?? TryGetString(d, "name") ?? Path.GetFileName(docpath),
                    Location = docpath,
                    DocPath = docpath,
                });
            }

            return result;
        }
        catch (Exception ex)
        {
            return new List<Document>();
        }
    }

    private static string? TryGetString(JsonElement el, string property)
    {
        if (el.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String)
            return prop.GetString();
        return null;
    }


    /// <summary>
    /// GET /v1/documents — todos los documentos del almacén global (árbol de carpetas).
    /// </summary>
    public async Task<List<Document>> GetAllDocumentsAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/documents");
            if (!response.IsSuccessStatusCode) return new List<Document>();

            var json = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GlobalDocumentsResponse>(json, JsonOptions);

            // Aplanar el árbol de carpetas en una lista plana de Document
            var docs = new List<Document>();
            foreach (var folder in result?.LocalFiles?.Items ?? new List<FolderItem>())
                FlattenFolder(folder, docs);

            return docs;
        }
        catch (Exception ex)
        {
            return new List<Document>();
        }
    }

    private static void FlattenFolder(FolderItem item, List<Document> docs, string parentPath = "")
    {
        if (item.Type == "file" || (item.Items.Count == 0 && !string.IsNullOrEmpty(item.Name) && item.Name.EndsWith(".json")))
        {
            // Construir la ruta relativa: "custom-documents/nombre-archivo.json"
            var docPath = !string.IsNullOrEmpty(item.DocPath)
                ? item.DocPath
                : !string.IsNullOrEmpty(item.Location)
                    ? item.Location
                    : string.IsNullOrEmpty(parentPath) ? item.Name : $"{parentPath}/{item.Name}";

            // Normalizar separadores
            docPath = docPath.Replace("\\", "/");

            docs.Add(new Document
            {
                Id = item.Id ?? string.Empty,
                Name = item.Name,
                Title = item.Title ?? item.Name,
                Location = docPath,
                DocPath = docPath,
                CreatedAt = item.CreatedAt ?? DateTime.MinValue
            });
        }

        // Recursión: pasar el nombre de la carpeta actual como parentPath
        var childParent = string.IsNullOrEmpty(parentPath) ? item.Name : $"{parentPath}/{item.Name}";
        foreach (var child in item.Items)
            FlattenFolder(child, docs, childParent);
    }


    private static string NormalizeDocPath(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        var idx = raw.IndexOf("custom-documents", StringComparison.OrdinalIgnoreCase);
        return (idx >= 0 ? raw[idx..] : raw).Replace("\\", "/");
    }

    // ─── Helpers ───────────────────────────────────────────────────────────────

    private static async Task<byte[]> ReadAllBytesAsync(FileResult fileResult)
    {
        using var stream = await fileResult.OpenReadAsync();
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms);
        return ms.ToArray();
    }
}

// ─── Response models ───────────────────────────────────────────────────────

/// <summary>
/// GET /v1/workspace/{slug} → { workspace: [ { documents: [...] } ] }
/// </summary>
public class WorkspaceSingleResponse
{
    public List<WorkspaceDetail> Workspace { get; set; } = new();
}

public class WorkspaceObjectResponse
{
    public WorkspaceDetail? Workspace { get; set; }
}

public class WorkspaceDetail
{
    public List<Document> Documents { get; set; } = new();
}