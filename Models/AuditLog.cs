using System;

namespace PilotApp.Models;

public enum AuditEventType
{
    LoginSuccess,
    LoginFailed,
    Logout,
    TaskCreated,
    TaskDeleted,
    DocumentCreated,
    DocumentDeleted,
    TimeEntryCreated,
    TimeEntryDeleted,
    UserCreated,
    UserDeleted
}

public sealed class AuditLogEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime OccurredAt { get; set; } = DateTime.Now;
    public string Login { get; set; } = string.Empty;
    public AuditEventType EventType { get; set; }
    public string Details { get; set; } = string.Empty;
    public bool Success { get; set; } = true;
}