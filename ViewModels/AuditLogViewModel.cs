using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using PilotApp.Models;
using PilotApp.Services;

namespace PilotApp.ViewModels;

public partial class AuditLogViewModel : ObservableObject
{
    private readonly AuditService _audit;

    [ObservableProperty] private string filterText = string.Empty;

    public AuditLogViewModel(AuditService audit)
    {
        _audit = audit;
    }

    public IEnumerable<AuditLogEntry> FilteredEntries =>
        _audit.GetAll()
            .Where(e => string.IsNullOrWhiteSpace(FilterText) ||
                        e.Login.Contains(FilterText, System.StringComparison.OrdinalIgnoreCase) ||
                        e.Details.Contains(FilterText, System.StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(e => e.OccurredAt);

    partial void OnFilterTextChanged(string value) => OnPropertyChanged(nameof(FilteredEntries));

    public void Refresh() => OnPropertyChanged(nameof(FilteredEntries));
}