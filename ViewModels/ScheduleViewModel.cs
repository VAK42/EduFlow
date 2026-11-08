using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services;
namespace EduFlow.WPF.ViewModels;
public record ScheduleCard(int LessonId, int ClassId, string TimeText, string ClassCode, string CourseCode,
  string CourseName, string RoomName, string TeacherName, string CategoryName, string AccentColor, LessonStatus Status);
public record ScheduleDay(string DayName, string DateText, bool IsToday, IReadOnlyList<ScheduleCard> Cards);
public partial class ScheduleViewModel : ViewModelBase
{
  private readonly IClassService _classes;
  public event Action<int, int>? AttendanceRequested;
  private static string ColorOf(CourseCategory? c) => c switch
  {
    CourseCategory.Language => "#2563EB",
    CourseCategory.Computer => "#7C3AED",
    CourseCategory.ShortCourse => "#D97706",
    _ => "#64748B"
  };
  public ObservableCollection<ScheduleDay> Days { get; } = new();
  [ObservableProperty] private DateTime _weekStart = StartOfWeek(DateTime.Today);
  [ObservableProperty] private DateTime? _filterDate = DateTime.Today;
  public DateTime? SelectedDate
  {
    get => FilterDate;
    set => FilterDate = value;
  }
  [ObservableProperty] private int _sessionCount;
  public string WeekText => $"Tuần {IsoWeek(WeekStart)}: {WeekStart:dd/MM} – {WeekStart.AddDays(6):dd/MM/yyyy}";
  public override string Title => "Thời Khóa Biểu";
  private static int IsoWeek(DateTime monday) => (monday.AddDays(3).DayOfYear - 1) / 7 + 1;
  public ScheduleViewModel(IClassService classes, IDialogService dialog) : base(dialog) => _classes = classes;
  private static DateTime StartOfWeek(DateTime d) => d.Date.AddDays(-(((int)d.DayOfWeek + 6) % 7));
  partial void OnFilterDateChanged(DateTime? value)
  {
    var date = value ?? DateTime.Today;
    var newWeek = StartOfWeek(date);
    if (newWeek != WeekStart)
    {
      WeekStart = newWeek;
      _ = LoadAsync();
    }
  }
  [RelayCommand]
  private void ClearFilterDate()
  {
    FilterDate = DateTime.Today;
  }
  public override Task LoadAsync() => RunAsync(async () =>
  {
    var sessions = await _classes.GetWeekScheduleAsync(WeekStart);
    SessionCount = sessions.Count;
    string[] names = { "Thứ 2", "Thứ 3", "Thứ 4", "Thứ 5", "Thứ 6", "Thứ 7", "Chủ Nhật" };
    Days.Clear();
    for (int i = 0; i < 7; i++)
    {
      var date = WeekStart.AddDays(i);
      var cards = sessions.Where(s => s.Date.Date == date).Select(ToCard).ToList();
      Days.Add(new ScheduleDay(names[i], date.ToString("dd/MM"), date == DateTime.Today, cards));
    }
    OnPropertyChanged(nameof(WeekText));
  });
  private static ScheduleCard ToCard(Lesson s)
  {
    var cls = s.ClassRoom!;
    var category = cls.Course?.Category;
    return new ScheduleCard(
      s.Id, cls.Id,
      $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}",
      cls.Code,
      cls.Course?.Code ?? cls.Code,
      cls.Course?.Name ?? "",
      s.PhysicalRoom?.Name ?? "",
      s.Teacher?.FullName ?? "",
      category?.ToText() ?? "",
      ColorOf(category),
      s.Status);
  }
  [RelayCommand]
  private void OpenAttendance(ScheduleCard? card)
  {
    if (card != null) AttendanceRequested?.Invoke(card.ClassId, card.LessonId);
  }
  [RelayCommand]
  private Task PreviousWeekAsync()
  {
    WeekStart = WeekStart.AddDays(-7);
    SelectedDate = WeekStart;
    return LoadAsync();
  }
  [RelayCommand]
  private Task NextWeekAsync()
  {
    WeekStart = WeekStart.AddDays(7);
    SelectedDate = WeekStart;
    return LoadAsync();
  }
  [RelayCommand]
  private Task ThisWeekAsync()
  {
    WeekStart = StartOfWeek(DateTime.Today);
    SelectedDate = DateTime.Today;
    return LoadAsync();
  }
}