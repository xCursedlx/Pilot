using Avalonia.Controls;
using Avalonia.Interactivity;
using PilotApp.Models;
using PilotApp.ViewModels;

namespace PilotApp.Views;

public partial class KanbanView : UserControl
{
    public KanbanView()
    {
        InitializeComponent();
    }

    private void OnMoveButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button btn)           return;
        if (btn.Tag is not string tagStr)        return;
        if (btn.DataContext is not TaskItem task) return;
        if (DataContext is not KanbanViewModel vm) return;

        if (!System.Enum.TryParse<AppTaskStatus>(tagStr, out var target)) return;

        vm.MoveTaskCommand.Execute((task, target));
        e.Handled = true;
    }
}