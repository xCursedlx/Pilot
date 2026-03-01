using System;

namespace PilotApp.Models;

public sealed class DocumentItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? FilePath { get; set; }
    public string Version { get; set; } = "v1";
    public string? LinkedTaskId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}