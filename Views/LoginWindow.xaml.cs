using System.Windows;
using EduFlow.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
namespace EduFlow.WPF.Views;
public partial class LoginWindow : Window
{
  private readonly LoginViewModel _vm;
  public LoginWindow(LoginViewModel vm)
  {
    InitializeComponent();
    DataContext = _vm = vm;
    _vm.LoginSucceeded += OpenMainWindow;
    Loaded += (_, _) => PasswordInput.Focus();
  }
  private async void LoginButton_Click(object sender, RoutedEventArgs e) =>
    await _vm.LoginCommand.ExecuteAsync(PasswordInput.Password);
  private void OpenMainWindow()
  {
    App.Services.GetRequiredService<MainWindow>().Show();
    Close();
  }
}