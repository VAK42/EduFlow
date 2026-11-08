using System.Linq;
using System.Windows;
using System.Windows.Controls;
using MaterialDesignThemes.Wpf;
namespace EduFlow.Services
{
  public interface IDialogService
  {
    void ShowInfo(string message);
    void ShowWarning(string message);
    void ShowError(string message);
    bool Confirm(string message);
    string? Prompt(string message, string hint);
  }
  public class DialogService : IDialogService
  {
    private const string Caption = "EduFlow";
    private readonly ISnackbarMessageQueue _snackbar;
    public DialogService(ISnackbarMessageQueue snackbar) => _snackbar = snackbar;
    public void ShowInfo(string message) => _snackbar.Enqueue(message);
    public void ShowWarning(string message) =>
      MessageBox.Show(message, Caption, MessageBoxButton.OK, MessageBoxImage.Warning);
    public void ShowError(string message) =>
      MessageBox.Show(message, Caption, MessageBoxButton.OK, MessageBoxImage.Error);
    public bool Confirm(string message) =>
      MessageBox.Show(message, Caption, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
    public string? Prompt(string message, string hint)
    {
      var input = new TextBox { Margin = new Thickness(0, 12, 0, 0), MinWidth = 380 };
      HintAssist.SetHint(input, hint);
      var ok = new Button { Content = "Đồng Ý", IsDefault = true, Margin = new Thickness(0, 0, 8, 0), MinWidth = 90 };
      var cancel = new Button { Content = "Hủy", IsCancel = true, MinWidth = 90 };
      var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
      buttons.Children.Add(ok);
      buttons.Children.Add(cancel);
      var panel = new StackPanel { Margin = new Thickness(24) };
      panel.Children.Add(new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap, MaxWidth = 420 });
      panel.Children.Add(input);
      panel.Children.Add(buttons);
      var window = new Window
      {
        Title = Caption, Content = panel, SizeToContent = SizeToContent.WidthAndHeight,
        ResizeMode = ResizeMode.NoResize, WindowStartupLocation = WindowStartupLocation.CenterOwner,
        Owner = Application.Current.Windows.OfType<Window>().FirstOrDefault(w => w.IsActive)
      };
      string? result = null;
      ok.Click += (_, _) => { result = input.Text; window.Close(); };
      cancel.Click += (_, _) => { result = null; window.Close(); };
      window.ShowDialog();
      return result?.Trim();
    }
  }
}