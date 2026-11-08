using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services;
using EduFlow.Services.Common;
using EduFlow.Services.Dtos;
namespace EduFlow.WPF.ViewModels;
public partial class AttendanceViewModel : ViewModelBase
{
  private readonly IClassService _classes;
  private readonly IAttendanceService _attendance;
  private int? _targetLessonId;
  public ObservableCollection<ClassRoom> Classes { get; } = new();
  public ObservableCollection<Lesson> Lessons { get; } = new();
  public ObservableCollection<AttendanceRow> Rows { get; } = new();
  public AttendanceStatus[] Statuses { get; } = Enum.GetValues(typeof(AttendanceStatus)).Cast<AttendanceStatus>().ToArray();
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(MarkAllPresentCommand))]
  [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
  private ClassRoom? _selectedClass;
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(MarkAllPresentCommand))]
  [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
  private Lesson? _selectedLesson;
  [ObservableProperty] private string? _content;
  public bool CanSave { get; }
  public override string Title => "Điểm Danh";
  public AttendanceViewModel(IClassService classes, IAttendanceService attendance, UserSession session, IDialogService dialog) : base(dialog)
  {
    _classes = classes;
    _attendance = attendance;
    CanSave = session.Can(Permission.TakeAttendance);
  }
  public async Task OpenLessonAsync(int classId, int lessonId)
  {
    Classes.Clear();
    foreach (var c in await _classes.GetMyClassesAsync()) Classes.Add(c);
    var cls = Classes.FirstOrDefault(c => c.Id == classId);
    if (cls != null)
    {
      _targetLessonId = lessonId;
      SelectedClass = cls;
    }
  }
  public override Task LoadAsync() => RunAsync(async () =>
  {
    Classes.Clear();
    foreach (var c in await _classes.GetMyClassesAsync()) Classes.Add(c);
  });
  partial void OnSelectedClassChanged(ClassRoom? value) => _ = RunAsync(async () =>
  {
    Lessons.Clear();
    Rows.Clear();
    if (value == null) return;
    foreach (var s in await _classes.GetLessonsAsync(value.Id)) Lessons.Add(s);
    if (_targetLessonId.HasValue)
    {
      SelectedLesson = Lessons.FirstOrDefault(l => l.Id == _targetLessonId.Value) ?? Lessons.FirstOrDefault();
      _targetLessonId = null;
    }
    else
    {
      SelectedLesson = Lessons.LastOrDefault(s => s.Date <= DateTime.Today) ?? Lessons.FirstOrDefault();
    }
    await LoadRowsAsync();
  });
  partial void OnSelectedLessonChanged(Lesson? value) => _ = RunAsync(LoadRowsAsync);
  private async Task LoadRowsAsync()
  {
    Rows.Clear();
    Content = SelectedLesson?.Content;
    if (SelectedLesson == null) return;
    foreach (var r in await _attendance.GetSheetAsync(SelectedLesson.Id)) Rows.Add(r);
  }
  private bool CanEditLesson() =>
    CanSave && SelectedLesson is { Status: not LessonStatus.Cancelled }
    && SelectedClass is { Status: ClassStatus.InProgress or ClassStatus.Finished };
  [RelayCommand(CanExecute = nameof(CanEditLesson))]
  private void MarkAllPresent()
  {
    foreach (var r in Rows) r.Status = AttendanceStatus.Present;
  }
  [RelayCommand(CanExecute = nameof(CanEditLesson))]
  private Task SaveAsync() => RunAsync(async () =>
  {
    if (SelectedLesson == null) throw new BusinessException("Hãy Chọn Buổi Học");
    if (string.IsNullOrWhiteSpace(Content)) throw new BusinessException("Vui Lòng Nhập Nội Dung Buổi Học");
    await _attendance.SaveAsync(SelectedLesson.Id, Rows, Content);
    int absent = Rows.Count(r => r.Status is AttendanceStatus.AbsentExcused or AttendanceStatus.AbsentUnexcused);
    Dialog.ShowInfo($"Đã Lưu Điểm Danh {SelectedLesson}: {Rows.Count - absent}/{Rows.Count} Có Mặt");
    int id = SelectedLesson.Id;
    Lessons.Clear();
    foreach (var l in await _classes.GetLessonsAsync(SelectedClass!.Id)) Lessons.Add(l);
    SelectedLesson = Lessons.FirstOrDefault(l => l.Id == id);
    await LoadRowsAsync();
  });
}