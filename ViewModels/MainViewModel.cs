using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Rag.Models;
using Rag.Services;
using Microsoft.Maui.Storage;
using Microsoft.Maui.Controls;

namespace Rag.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly AnythingLlmService _apiService;

    // --- Workspaces State ---
    [ObservableProperty]
    public partial ObservableCollection<Workspace> Workspaces { get; set; }

    [ObservableProperty]
    public partial Workspace? SelectedWorkspace { get; set; }

    [ObservableProperty]
    public partial bool IsBusyLoadingWorkspaces { get; set; }

    // --- Chat State ---
    [ObservableProperty]
    public partial ObservableCollection<ChatMessage> Messages { get; set; }

    [ObservableProperty]
    public partial string InputText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsTyping { get; set; }

    // --- New Workspace State ---
    [ObservableProperty]
    public partial string NewWorkspaceName { get; set; } = string.Empty;

    // --- Workspace Details State ---
    [ObservableProperty]
    public partial ObservableCollection<Document> WorkspaceDocuments { get; set; }

    /// <summary>Todos los documentos del almacén global de AnythingLLM.</summary>
    [ObservableProperty]
    public partial ObservableCollection<Document> AllDocuments { get; set; }

    [ObservableProperty]
    public partial bool IsViewChatActive { get; set; } = true;

    [ObservableProperty]
    public partial bool IsViewDocsActive { get; set; } = false;

    // --- Modo de interacción ---
    /// <summary>"chat" (con historial) o "query" (consulta puntual sin contexto).</summary>
    [ObservableProperty]
    public partial string ChatMode { get; set; } = "chat";

    public bool IsChatMode => ChatMode == "chat";
    public bool IsQueryMode => ChatMode == "query";

    /// <summary>System prompt inyectado en cada conversación del workspace.</summary>
    [ObservableProperty]
    public partial string SystemPrompt { get; set; } = string.Empty;

    public bool HasSelectedWorkspace => SelectedWorkspace != null;
    public bool HasNoSelectedWorkspace => SelectedWorkspace == null;

    public MainViewModel(AnythingLlmService apiService)
    {
        _apiService = apiService;
        Workspaces = new ObservableCollection<Workspace>();
        Messages = new ObservableCollection<ChatMessage>();
        WorkspaceDocuments = new ObservableCollection<Document>();
        AllDocuments = new ObservableCollection<Document>();
    }

    partial void OnSelectedWorkspaceChanged(Workspace value)
    {
        OnPropertyChanged(nameof(HasSelectedWorkspace));
        OnPropertyChanged(nameof(HasNoSelectedWorkspace));
        Messages.Clear();
        WorkspaceDocuments.Clear();
        if (value != null)
        {
            LoadDocumentsAsync().ConfigureAwait(false);
        }
    }

    [RelayCommand]
    public void ShowChatView()
    {
        IsViewChatActive = true;
        IsViewDocsActive = false;
    }

    [RelayCommand]
    public void ShowDocsView()
    {
        IsViewChatActive = false;
        IsViewDocsActive = true;
    }

    // ─── Modo Chat / Query ─────────────────────────────────────────────────────

    // Backup del historial 
    private readonly List<ChatMessage> _chatHistoryBackup = new();

    [RelayCommand]
    public void SetChatMode()
    {
        if (ChatMode == "chat") return;

        ChatMode = "chat";
        OnPropertyChanged(nameof(IsChatMode));
        OnPropertyChanged(nameof(IsQueryMode));

        // Restaurar historial del chat guardado
        Messages.Clear();
        foreach (var msg in _chatHistoryBackup)
            Messages.Add(msg);
    }

    [RelayCommand]
    public void SetQueryMode()
    {
        if (ChatMode == "query") return;

        // Guardar historial del chat antes de limpiar
        _chatHistoryBackup.Clear();
        _chatHistoryBackup.AddRange(Messages);

        ChatMode = "query";
        OnPropertyChanged(nameof(IsChatMode));
        OnPropertyChanged(nameof(IsQueryMode));
        Messages.Clear(); // query empieza siempre limpio
    }

    // ─── System Prompt ─────────────────────────────────────────────────────────

    [RelayCommand]
    public async Task EditSystemPromptAsync()
    {
        if (SelectedWorkspace == null || Application.Current?.MainPage == null) return;

        string current = SystemPrompt;
        string? result = await Application.Current.MainPage.DisplayPromptAsync(
            "System Prompt",
            "Texto inyectado al inicio de cada conversación:",
            initialValue: current,
            maxLength: 2000,
            keyboard: Keyboard.Text);

        if (result == null) return; // cancelado

        SystemPrompt = result;

        bool saved = await _apiService.UpdateWorkspaceSettingsAsync(
            SelectedWorkspace.Slug, SystemPrompt, ChatMode);

        _ = ShowNotificationAsync(saved
            ? "✓ System prompt guardado."
            : "✗ Error al guardar el system prompt.");
    }

    // --- Workspaces Commands ---

    [RelayCommand]
    public async Task LoadWorkspacesAsync()
    {
        if (IsBusyLoadingWorkspaces) return;
        IsBusyLoadingWorkspaces = true;

        var data = await _apiService.GetWorkspacesAsync();
        Workspaces.Clear();
        foreach (var ws in data)
        {
            Workspaces.Add(ws);
        }

        IsBusyLoadingWorkspaces = false;
    }
    // ...

    [RelayCommand]
    public async Task CreateWorkspaceAsync()
    {
        if (string.IsNullOrWhiteSpace(NewWorkspaceName)) return;

        bool success = await _apiService.CreateWorkspaceAsync(NewWorkspaceName);
        if (success)
        {
            NewWorkspaceName = string.Empty;
            await LoadWorkspacesAsync();
        }
        else
        {
            if (Application.Current?.MainPage != null)
                await Application.Current.MainPage.DisplayAlert("Error", "No se pudo crear el workspace", "OK");
        }
    }

    [RelayCommand]
    public async Task UpdateWorkspaceAsync(Workspace workspace)
    {
        if (workspace == null) return;

        if (Application.Current?.MainPage != null)
        {
            string newName = await Application.Current.MainPage.DisplayPromptAsync("Editar", "Nuevo nombre del workspace:", initialValue: workspace.Name);
            if (!string.IsNullOrWhiteSpace(newName) && newName != workspace.Name)
            {
                bool success = await _apiService.UpdateWorkspaceAsync(workspace.Slug, newName);
                if (success)
                {
                    await LoadWorkspacesAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No se pudo actualizar el workspace", "OK");
                }
            }
        }
    }

    [RelayCommand]
    public async Task DeleteWorkspaceAsync(Workspace workspace)
    {
        if (workspace == null) return;

        if (Application.Current?.MainPage != null)
        {
            bool confirm = await Application.Current.MainPage.DisplayAlert("Confirmar", $"¿Seguro que deseas eliminar '{workspace.Name}'?", "Sí", "No");
            if (confirm)
            {
                bool success = await _apiService.DeleteWorkspaceAsync(workspace.Slug);
                if (success)
                {
                    if (SelectedWorkspace?.Slug == workspace.Slug) SelectedWorkspace = null;
                    await LoadWorkspacesAsync();
                }
                else
                {
                    await Application.Current.MainPage.DisplayAlert("Error", "No se pudo eliminar el workspace", "OK");
                }
            }
        }
    }

    // --- Chat and Document Commands ---

    [RelayCommand]
    public async Task LoadDocumentsAsync()
    {
        if (SelectedWorkspace == null) return;

        // Cargar documentos del workspace (con embedding)
        var workspaceDocs = await _apiService.GetDocumentsAsync(SelectedWorkspace.Slug);
        WorkspaceDocuments.Clear();
        foreach (var doc in workspaceDocs)
            WorkspaceDocuments.Add(doc);

        // Cargar todos los documentos del almacén global
        var allDocs = await _apiService.GetAllDocumentsAsync();
        AllDocuments.Clear();
        foreach (var doc in allDocs)
            AllDocuments.Add(doc);
    }

    /// <summary>Mueve un documento del almacén global al workspace (update-embeddings adds).</summary>
    [RelayCommand]
    public async Task AddToWorkspaceAsync(Document document)
    {
        if (document == null || SelectedWorkspace == null) return;

        var raw = !string.IsNullOrEmpty(document.DocPath) ? document.DocPath : document.Location;
        var location = NormalizeDocPath(raw);

        Console.WriteLine($"AddToWorkspace → location enviado: {location}");

        bool success = await _apiService.UpdateEmbeddingsAsync(SelectedWorkspace.Slug, adds: new[] { location });
        if (success)
        {
            // Esperar un momento para que el servidor procese el embedding
            await Task.Delay(800);
            await LoadDocumentsAsync();
        }
        else if (Application.Current?.MainPage != null)
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo añadir el documento al workspace.\nRuta: {location}", "OK");
    }

    /// <summary>Quita un documento del workspace (update-embeddings deletes) sin borrarlo del global.</summary>
    [RelayCommand]
    public async Task RemoveFromWorkspaceAsync(Document document)
    {
        if (document == null || SelectedWorkspace == null) return;

        var raw = !string.IsNullOrEmpty(document.DocPath) ? document.DocPath : document.Location;
        var location = NormalizeDocPath(raw);

        Console.WriteLine($"RemoveFromWorkspace → location enviado: {location}");

        bool success = await _apiService.UpdateEmbeddingsAsync(SelectedWorkspace.Slug, deletes: new[] { location });
        if (success)
            WorkspaceDocuments.Remove(document);
        else if (Application.Current?.MainPage != null)
            await Application.Current.MainPage.DisplayAlert("Error", $"No se pudo quitar el documento.\nRuta: {location}", "OK");
    }

    /// <summary>
    /// Normaliza la ruta del documento a formato relativo "custom-documents/..."
    /// igual que hace el compañero al subir archivos.
    /// </summary>
    private static string NormalizeDocPath(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        var idx = raw.IndexOf("custom-documents", StringComparison.OrdinalIgnoreCase);
        return (idx >= 0 ? raw[idx..] : raw).Replace("\\", "/");
    }


    [RelayCommand]
    public async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(InputText) || IsTyping || SelectedWorkspace == null) return;

        string userMessage = InputText;
        InputText = string.Empty;

        // En modo query no acumulamos historial visual, limpiamos antes de mostrar el mensaje actual
        if (ChatMode == "query") Messages.Clear();

        Messages.Add(new ChatMessage { Role = "user", Content = userMessage });

        IsTyping = true;

        var aiResponse = await _apiService.SendChatMessageAsync(SelectedWorkspace.Slug, userMessage, ChatMode);

        Messages.Add(aiResponse);
        IsTyping = false;
    }

    [RelayCommand]
    public async Task UploadDocumentAsync()
    {
        if (SelectedWorkspace == null) return;

        try
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Selecciona un documento o imagen"
            });

            if (result != null)
            {
                IsTyping = true;
                bool success = await _apiService.UploadDocumentAsync(SelectedWorkspace.Slug, result);
                IsTyping = false;

                if (success)
                {
                    _ = ShowNotificationAsync($"✓ '{result.FileName}' subido y añadido al workspace.");
                    await LoadDocumentsAsync();
                }
                else
                {
                    _ = ShowNotificationAsync($"✗ Error al subir '{result.FileName}'.");
                }
            }
        }
        catch (Exception ex)
        {
            IsTyping = false;
            _ = ShowNotificationAsync($"✗ Error: {ex.Message}");
        }
    }

    /// <summary>
    /// Añade un mensaje de notificación al chat y lo elimina automáticamente tras 3 segundos.
    /// </summary>
    private async Task ShowNotificationAsync(string text, int delayMs = 3000)
    {
        var msg = new ChatMessage { Role = "notification", Content = text };
        Messages.Add(msg);
        await Task.Delay(delayMs);
        Messages.Remove(msg);
    }
}
