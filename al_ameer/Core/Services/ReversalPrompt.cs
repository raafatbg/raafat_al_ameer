using System.Windows;
using System.Windows.Controls;

namespace al_ameer.Services;

public static class ReversalPrompt
{
    public static string? Ask(Window? owner, string title)
    {
        var input = new TextBox { Height = 70, MaxLength = 400, AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 10, 0, 12) };
        var confirm = new Button { Content = "Confirm reversal / تأكيد الإلغاء", Width = 200,
            Height = 36, HorizontalAlignment = HorizontalAlignment.Right };
        var panel = new StackPanel { Margin = new Thickness(20) };
        panel.Children.Add(new TextBlock { Text = "Reason / السبب (required)", FontWeight = FontWeights.SemiBold });
        panel.Children.Add(input);
        panel.Children.Add(confirm);
        var dialog = new Window { Title = title, Width = 420, Height = 205, Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner, ResizeMode = ResizeMode.NoResize };
        if (owner is not null) dialog.Owner = owner;
        confirm.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(input.Text)) { MessageBox.Show("Enter a reason."); return; }
            dialog.DialogResult = true;
        };
        return dialog.ShowDialog() == true ? input.Text.Trim() : null;
    }
}
