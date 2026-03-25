using System;

namespace PilotApp.Models;

public sealed class StatusHistoryEntry
{
    public DateTime      ChangedAt  { get; set; } = DateTime.Now;
    public AppTaskStatus OldStatus  { get; set; }
    public AppTaskStatus NewStatus  { get; set; }
    public string?       ChangedBy  { get; set; }
}