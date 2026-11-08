using System.Configuration;
using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using EduFlow.Models.Common;
using EduFlow.Services;
using EduFlow.WPF.ViewModels;
using EduFlow.WPF.Views;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
namespace EduFlow.WPF;
public partial class App : Application
{
  public static IServiceProvider Services { get; private set; } = null!;
  protected override async void OnStartup(StartupEventArgs e)
  {
    base.OnStartup(e);
    DispatcherUnhandledException += OnUnhandledException;
    var vi = new CultureInfo("vi-VN");
    CultureInfo.CurrentCulture = vi;
    CultureInfo.CurrentUICulture = vi;
    CultureInfo.DefaultThreadCurrentCulture = vi;
    CultureInfo.DefaultThreadCurrentUICulture = vi;
    var language = XmlLanguage.GetLanguage(vi.IetfLanguageTag);
    FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(language));
    string connectionString = ConfigurationManager.ConnectionStrings["EduFlow"]?.ConnectionString
      ?? throw new InvalidOperationException("Thiếu Chuỗi Kết Nối EduFlow Trong App.config");
    Services = ConfigureServices(connectionString);
    try
    {
      await Services.GetRequiredService<DatabaseInitializer>().InitializeAsync();
    }
    catch (BusinessException ex)
    {
      MessageBox.Show(ex.Message, "EduFlow", MessageBoxButton.OK, MessageBoxImage.Warning);
      Shutdown();
      return;
    }
    catch (Exception ex)
    {
      MessageBox.Show("Không Thể Kết Nối Hoặc Khởi Tạo CSDL SQLite\nKiểm Tra Lại Quyền Ghi Thư Mục Hoặc Tệp EduFlow.db\n\n" + ex.Message, "EduFlow", MessageBoxButton.OK, MessageBoxImage.Error);
      Shutdown();
      return;
    }
    Services.GetRequiredService<LoginWindow>().Show();
  }
  private static IServiceProvider ConfigureServices(string connectionString)
  {
    var services = new ServiceCollection();
    services.AddEduFlowServices(connectionString);
    services.AddSingleton<ISnackbarMessageQueue>(_ => new SnackbarMessageQueue(TimeSpan.FromSeconds(3)));
    services.AddSingleton<IDialogService, DialogService>();
    services.AddTransient<LoginViewModel>();
    services.AddTransient<MainViewModel>();
    services.AddTransient<DashboardViewModel>();
    services.AddTransient<StudentsViewModel>();
    services.AddTransient<UsersViewModel>();
    services.AddTransient<ScoresViewModel>();
    services.AddTransient<SettingsViewModel>();
    services.AddTransient<CoursesViewModel>();
    services.AddTransient<ClassesViewModel>();
    services.AddTransient<TuitionFeeViewModel>();
    services.AddTransient<SalaryViewModel>();
    services.AddTransient<AttendanceViewModel>();
    services.AddTransient<ScheduleViewModel>();
    services.AddTransient<LoginWindow>();
    services.AddTransient<MainWindow>();
    return services.BuildServiceProvider();
  }
  private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
  {
    MessageBox.Show("Lỗi Không Mong Muốn: " + e.Exception.Message, "EduFlow", MessageBoxButton.OK, MessageBoxImage.Error);
    e.Handled = true;
  }
}