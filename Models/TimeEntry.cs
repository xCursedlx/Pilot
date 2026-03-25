using System;

namespace PilotApp.Models;

public sealed class TimeEntry
{
    public Guid      Id        { get; set; } = Guid.NewGuid();
    public Guid?     TaskId    { get; set; }
    public string    TaskTitle { get; set; } = string.Empty;
    public string?   User      { get; set; }
    public DateTime  Date      { get; set; } = DateTime.Today;
    public double    Hours     { get; set; }
    public string?   Comment   { get; set; }
}