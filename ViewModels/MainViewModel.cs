using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services;
using EduFlow.Services.Common;
using MaterialDesignThemes.Wpf;
using Microsoft.Extensions.DependencyInjection;
namespace EduFlow.WPF.ViewModels;
public record NavItem(string Group, string Title, PackIconKind Icon, Type PageType, Func<User, bool> IsVisible);
public partial class MainViewModel : ViewModelBase
{
  private readonly IServiceProvider _provider;
  private readonly UserSession _session;
  private readonly IAuthService _auth;
  private static bool All(User _) => true;
  private static readonly NavItem[] AllItems =
  {
    new("Tổng Quan", "Tổng Quan", PackIconKind.ViewDashboard, typeof(DashboardViewModel), u => u.Role == UserRole.Admin),
    new("Tổng Quan", "Thời Khóa Biểu", PackIconKind.CalendarWeek, typeof(ScheduleViewModel), All),
    new("Đào Tạo", "Học Viên", PackIconKind.AccountSchool, typeof(StudentsViewModel), u => !u.IsTeacher),
    new("Đào Tạo", "Khóa Học & Phòng", PackIconKind.BookOpenPageVariant, typeof(CoursesViewModel), u => !u.IsTeacher),
    new("Đào Tạo", "Lớp Học & Lịch", PackIconKind.GoogleClassroom, typeof(ClassesViewModel), u => !u.IsTeacher),
    new("Đào Tạo", "Điểm Danh", PackIconKind.ClipboardCheckOutline, typeof(AttendanceViewModel), u => u.Can(Permission.TakeAttendance)),
    new("Đào Tạo", "Bảng Điểm", PackIconKind.Numeric10BoxMultipleOutline, typeof(ScoresViewModel), u => u.Can(Permission.EnterScores)),
    new("Tài Chính", "Học Phí", PackIconKind.CashRegister, typeof(TuitionFeeViewModel), u => u.Role == UserRole.Admin),
    new("Tài Chính", "Lương Bổng", PackIconKind.CashMultiple, typeof(SalaryViewModel), All),
    new("Hệ Thống", "Tài Khoản", PackIconKind.AccountKey, typeof(UsersViewModel), u => u.Role == UserRole.Admin),
    new("Hệ Thống", "Cài Đặt", PackIconKind.CogOutline, typeof(SettingsViewModel), All),
  };
  public ObservableCollection<NavItem> MenuItems { get; } = new();
  public ISnackbarMessageQueue MessageQueue { get; }
  [ObservableProperty]
  private NavItem? _selectedMenu;
  [ObservableProperty]
  private ViewModelBase? _currentPage;
  public event Action? LogoutRequested;
  public override string Title => "EduFlow - Quản Lý Trung Tâm Đào Tạo";
  public string UserName => _session.CurrentUser?.FullName ?? "";
  public string UserRoleText => _session.CurrentUser?.Role.ToText() ?? "";
  public string TodayText
  {
    get
    {
      var vi = new CultureInfo("vi-VN");
      string day = vi.DateTimeFormat.GetDayName(DateTime.Today.DayOfWeek);
      return $"{vi.TextInfo.ToTitleCase(day)}, {DateTime.Today:dd/MM/yyyy}";
    }
  }
  public MainViewModel(IServiceProvider provider, UserSession session, IAuthService auth,
    ISnackbarMessageQueue messageQueue, IDialogService dialog) : base(dialog)
  {
    _provider = provider;
    _session = session;
    _auth = auth;
    MessageQueue = messageQueue;
  }
  public override Task LoadAsync()
  {
    var user = _session.RequireUser();
    MenuItems.Clear();
    foreach (var item in AllItems.Where(i => i.IsVisible(user)))
      MenuItems.Add(item);
    SelectedMenu = MenuItems.FirstOrDefault();
    OnPropertyChanged(nameof(UserName));
    OnPropertyChanged(nameof(UserRoleText));
    return Task.CompletedTask;
  }
  partial void OnSelectedMenuChanged(NavItem? value)
  {
    if (value == null) return;
    var page = (ViewModelBase)_provider.GetRequiredService(value.PageType);
    if (page is ScheduleViewModel scheduleVm)
    {
      scheduleVm.AttendanceRequested += (classId, lessonId) =>
      {
        var attNav = MenuItems.FirstOrDefault(m => m.PageType == typeof(AttendanceViewModel));
        if (attNav != null)
        {
          SelectedMenu = attNav;
          if (CurrentPage is AttendanceViewModel attVm)
            _ = attVm.OpenLessonAsync(classId, lessonId);
        }
      };
    }
    CurrentPage = page;
    _ = page.LoadAsync();
  }
  [RelayCommand]
  private void Logout()
  {
    if (!Dialog.Confirm("Bạn Muốn Đăng Xuất?")) return;
    _auth.Logout();
    LogoutRequested?.Invoke();
  }
}