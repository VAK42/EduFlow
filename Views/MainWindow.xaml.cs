using System.Windows;
using EduFlow.WPF.ViewModels;
using Microsoft.Extensions.DependencyInjection;
namespace EduFlow.WPF.Views;
public partial class MainWindow : Window
{
  public MainWindow(MainViewModel vm)
  {
    InitializeComponent();
    DataContext = vm;
    vm.LogoutRequested += () =>
    {
      App.Services.GetRequiredService<LoginWindow>().Show();
      Close();
    };
    Loaded += async (_, _) => await vm.LoadAsync();
  }
}