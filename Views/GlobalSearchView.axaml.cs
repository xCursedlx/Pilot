using Avalonia.Controls;
using Avalonia.Interactivity;
using PilotApp.ViewModels;

namespace PilotApp.Views;

public partial class GlobalSearchView : UserControl
{
    public GlobalSearchView()
    {
        InitializeComponent();
    }

    private void OnCloseClicked(object? sender, RoutedEventArgs e)
    {
        if (DataContext is GlobalSearchViewModel vm)
            vm.CloseSearch();
    }
}