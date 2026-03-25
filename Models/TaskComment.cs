using System;

namespace PilotApp.Models;

public sealed class TaskComment
{
    public Guid     Id        { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public string   Author    { get; set; } = string.Empty;
    public string   Text      { get; set; } = string.Empty;
}