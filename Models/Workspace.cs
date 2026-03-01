namespace Rag.Models;

public class Workspace
{
    public string Name { get; set; } = string.Empty;
    public string Slug { get; set; } = string.Empty;
}

public class WorkspaceResponse
{
    public List<Workspace> Workspaces { get; set; } = new();
}