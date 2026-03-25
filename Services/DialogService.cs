using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace PilotApp.Services;

public sealed class DialogService : IDialogService
{
    public Task<bool> ConfirmAsync(string title, string message)
    {
        var tcs = new TaskCompletionSource<bool>();

        Dispatcher.UIThread.Post(() =>
        {
            Window? dialog = null;

            var text = new TextBlock
            {
                Text         = message,
                TextWrapping = TextWrapping.Wrap,
                Margin       = new Thickness(20, 20, 20, 16),
                FontSize     = 14
            };

            var yes = new Button
            {
                Content    = "Да, удалить",
                Width      = 120,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            var no = new Button
            {
                Content = "Отмена",
                Width   = 100,
                HorizontalContentAlignment = HorizontalAlignment.Center
            };

            yes.Click += (_, _) => { tcs.TrySetResult(true);  dialog?.Close(); };
            no.Click  += (_, _) => { tcs.TrySetResult(false); dialog?.Close(); };

            var buttons = new StackPanel
            {
                Orientation         = Orientation.Horizontal,
                Spacing             = 10,
                Margin              = new Thickness(20, 0, 20, 20),
                HorizontalAlignment = HorizontalAlignment.Right,
                Children            = { yes, no }
            };

            dialog = new Window
            {
                Title                 = title,
                Width                 = 380,
                Height                = 160,
                CanResize             = false,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content               = new StackPanel { Children = { text, buttons } }
            };

            var owner = FilePickerHelper.TopLevel as Window;
            if (owner is not null)
                dialog.ShowDialog(owner);
            else
                dialog.Show();
        });

        return tcs.Task;
    }
}