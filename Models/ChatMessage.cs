namespace Rag.Models;

// Esta clase representa un mensaje en la pantalla (tuyo o de la IA)
public class ChatMessage
{
    public string Role { get; set; } = string.Empty; 
    public string Content { get; set; } = string.Empty;
    public List<Chunk> Chunks { get; set; } = new();

    public bool IsNotification => Role == "notification";
    public bool HasChunks => !IsNotification && Chunks != null && Chunks.Count > 0;
    public LayoutOptions Alignment => Role == "user" ? LayoutOptions.End : LayoutOptions.Start;
    public Color BackgroundColor => Role switch
    {
        "user"         => Color.FromArgb("#1E40AF"),   // azul oscuro - usuario
        "notification" => Color.FromArgb("#064E3B"),   // verde oscuro - sistema
        _              => Color.FromArgb("#1E293B"),   // gris oscuro  - IA
    };
}

public class Chunk
{
    public string Title { get; set; } = string.Empty; // Nombre del documento
    public string Text { get; set; } = string.Empty; // El fragmento de texto
    public double Score { get; set; } // La puntuación de similitud
}

public class AnythingLlmChatResponse
{
    public string textResponse { get; set; } = string.Empty;
    public List<AnythingLlmSource> sources { get; set; } = new();
}

public class AnythingLlmSource
{
    public string title { get; set; } = string.Empty;
    public string text { get; set; } = string.Empty;
    public double score { get; set; }
}