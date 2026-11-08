using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services;
using EduFlow.Services.Common;
namespace EduFlow.WPF.ViewModels;
public partial class ClassesViewModel : ViewModelBase
{
  private readonly IClassService _classes;
  private readonly ICourseService _courses;
  private readonly IUserService _users;
  private readonly ICatalogService _catalog;
  public ObservableCollection<ClassRoom> Classes { get; } = new();
  public ObservableCollection<Lesson> Lessons { get; } = new();
  public ObservableCollection<Course> Courses { get; } = new();
  public ObservableCollection<User> Teachers { get; } = new();
  public ObservableCollection<Room> Rooms { get; } = new();
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(StartClassCommand))]
  [NotifyCanExecuteChangedFor(nameof(FinishClassCommand))]
  [NotifyCanExecuteChangedFor(nameof(CancelClassCommand))]
  [NotifyCanExecuteChangedFor(nameof(DeleteClassCommand))]
  [NotifyCanExecuteChangedFor(nameof(CancelLessonCommand))]
  private ClassRoom? _selectedClass;
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(CancelLessonCommand))]
  private Lesson? _selectedLesson;
  public bool CanManage { get; }
  public bool CanCancel { get; }
  [ObservableProperty] private bool _isEditing;
  [ObservableProperty] private int _editId;
  [ObservableProperty] private Course? _newCourse;
  [ObservableProperty] private User? _newTeacher;
  [ObservableProperty] private Room? _newRoom;
  [ObservableProperty] private DateTime _newStartDate = DateTime.Today.AddDays(7);
  [ObservableProperty] private string _newStartTime = "18:00";
  [ObservableProperty] private string _newEndTime = "19:30";
  [ObservableProperty] private int _newMinStudents = 5;
  [ObservableProperty] private int _newMaxStudents = 20;
  [ObservableProperty] private bool _mon = true;
  [ObservableProperty] private bool _tue;
  [ObservableProperty] private bool _wed = true;
  [ObservableProperty] private bool _thu;
  [ObservableProperty] private bool _fri = true;
  [ObservableProperty] private bool _sat;
  [ObservableProperty] private bool _sun;
  public string FormTitle => EditId == 0 ? "Mở Lớp Mới" : "Cập Nhật Lớp Học";
  public override string Title => "Lớp Học & Lịch Học";
  public ClassesViewModel(IClassService classes, ICourseService courses, IUserService users,
    ICatalogService catalog, UserSession session, IDialogService dialog) : base(dialog)
  {
    _classes = classes;
    _courses = courses;
    _users = users;
    _catalog = catalog;
    CanManage = session.Can(Permission.ManageClasses);
    CanCancel = session.Can(Permission.CancelClass);
  }
  public override Task LoadAsync() => RunAsync(async () =>
  {
    if (CanManage)
    {
      Courses.Clear();
      foreach (var c in await _courses.GetAllAsync(activeOnly: true)) Courses.Add(c);
      Rooms.Clear();
      foreach (var r in await _catalog.GetRoomsAsync()) Rooms.Add(r);
      Teachers.Clear();
      foreach (var t in await _users.GetTeachersAsync()) Teachers.Add(t);
    }
    await LoadClassesAsync();
  });
  private async Task LoadClassesAsync()
  {
    Classes.Clear();
    Lessons.Clear();
    foreach (var c in await _classes.GetAllAsync()) Classes.Add(c);
  }
  partial void OnSelectedClassChanged(ClassRoom? value)
  {
    if (value != null)
    {
      EditId = value.Id;
      NewCourse = Courses.FirstOrDefault(c => c.Id == value.CourseId);
      NewTeacher = Teachers.FirstOrDefault(t => t.Id == value.TeacherId);
      NewRoom = Rooms.FirstOrDefault(r => r.Id == value.PhysicalRoomId);
      NewStartDate = value.StartDate;
      NewStartTime = value.StartTime.ToString(@"hh\:mm");
      NewEndTime = value.EndTime.ToString(@"hh\:mm");
      NewMinStudents = value.MinStudents;
      NewMaxStudents = value.MaxStudents;
      Mon = value.Days.HasFlag(WeekDays.Monday);
      Tue = value.Days.HasFlag(WeekDays.Tuesday);
      Wed = value.Days.HasFlag(WeekDays.Wednesday);
      Thu = value.Days.HasFlag(WeekDays.Thursday);
      Fri = value.Days.HasFlag(WeekDays.Friday);
      Sat = value.Days.HasFlag(WeekDays.Saturday);
      Sun = value.Days.HasFlag(WeekDays.Sunday);
    }
    _ = LoadLessonsAsync();
  }
  partial void OnEditIdChanged(int value) => OnPropertyChanged(nameof(FormTitle));
  private async Task LoadLessonsAsync()
  {
    Lessons.Clear();
    if (SelectedClass == null) return;
    try
    {
      foreach (var s in await _classes.GetLessonsAsync(SelectedClass.Id)) Lessons.Add(s);
    }
    catch (Exception ex)
    {
      Dialog.ShowError(ex.Message);
    }
  }
  private WeekDays SelectedDays()
  {
    var d = WeekDays.None;
    if (Mon) d |= WeekDays.Monday;
    if (Tue) d |= WeekDays.Tuesday;
    if (Wed) d |= WeekDays.Wednesday;
    if (Thu) d |= WeekDays.Thursday;
    if (Fri) d |= WeekDays.Friday;
    if (Sat) d |= WeekDays.Saturday;
    if (Sun) d |= WeekDays.Sunday;
    return d;
  }
  private bool CanStartClass() => SelectedClass is { Status: ClassStatus.Open };
  private bool CanFinishClass() => SelectedClass is { Status: ClassStatus.InProgress };
  private bool CanCancelClass() => SelectedClass is { Status: ClassStatus.Open };
  private bool CanDeleteClass() => SelectedClass != null;
  private bool CanCancelLesson() =>
    SelectedLesson is { Status: LessonStatus.Scheduled } && SelectedClass is { Status: ClassStatus.Open or ClassStatus.InProgress };
  [RelayCommand]
  private void NewClass()
  {
    SelectedClass = null;
    EditId = 0;
    NewCourse = Courses.FirstOrDefault();
    NewTeacher = Teachers.FirstOrDefault();
    NewRoom = Rooms.FirstOrDefault();
    NewStartDate = DateTime.Today.AddDays(7);
    NewStartTime = "18:00";
    NewEndTime = "19:30";
    NewMinStudents = 5;
    NewMaxStudents = 20;
    Mon = true;
    Tue = false;
    Wed = true;
    Thu = false;
    Fri = true;
    Sat = false;
    Sun = false;
    IsEditing = true;
  }
  [RelayCommand]
  private void Edit(ClassRoom? target = null)
  {
    var c = target ?? SelectedClass;
    if (c == null) return;
    SelectedClass = c;
    EditId = c.Id;
    NewCourse = Courses.FirstOrDefault(x => x.Id == c.CourseId);
    NewTeacher = Teachers.FirstOrDefault(x => x.Id == c.TeacherId);
    NewRoom = Rooms.FirstOrDefault(x => x.Id == c.PhysicalRoomId);
    NewStartDate = c.StartDate;
    NewStartTime = c.StartTime.ToString(@"hh\:mm");
    NewEndTime = c.EndTime.ToString(@"hh\:mm");
    NewMinStudents = c.MinStudents;
    NewMaxStudents = c.MaxStudents;
    Mon = c.Days.HasFlag(WeekDays.Monday);
    Tue = c.Days.HasFlag(WeekDays.Tuesday);
    Wed = c.Days.HasFlag(WeekDays.Wednesday);
    Thu = c.Days.HasFlag(WeekDays.Thursday);
    Fri = c.Days.HasFlag(WeekDays.Friday);
    Sat = c.Days.HasFlag(WeekDays.Saturday);
    Sun = c.Days.HasFlag(WeekDays.Sunday);
    IsEditing = true;
  }
  [RelayCommand]
  private void CancelEdit()
  {
    IsEditing = false;
  }
  [RelayCommand]
  private Task CreateClassAsync() => RunAsync(async () =>
  {
    if (NewCourse == null) throw new BusinessException("Vui Lòng Chọn Khóa Học");
    if (NewTeacher == null) throw new BusinessException("Vui Lòng Chọn Giáo Viên");
    if (NewRoom == null) throw new BusinessException("Vui Lòng Chọn Phòng Học");
    if (SelectedDays() == WeekDays.None) throw new BusinessException("Vui Lòng Chọn Ít Nhất 1 Ngày Học Trong Tuần");
    if (string.IsNullOrWhiteSpace(NewStartTime)) throw new BusinessException("Vui Lòng Nhập Giờ Bắt Đầu");
    if (string.IsNullOrWhiteSpace(NewEndTime)) throw new BusinessException("Vui Lòng Nhập Giờ Kết Thúc");
    if (!TimeSpan.TryParse(NewStartTime, out var start) || !TimeSpan.TryParse(NewEndTime, out var end))
      throw new BusinessException("Giờ Học Phải Có Dạng HH:mm, Ví Dụ 18:00");
    if (NewMinStudents <= 0) throw new BusinessException("Sĩ Số Tối Thiểu Phải Lớn Hơn 0");
    if (NewMaxStudents <= 0) throw new BusinessException("Sĩ Số Tối Đa Phải Lớn Hơn 0");
    if (NewMinStudents > NewMaxStudents) throw new BusinessException("Sĩ Số Tối Thiểu Không Được Lớn Hơn Sĩ Số Tối Đa");
    var input = new ClassRoom
    {
      Id = EditId,
      CourseId = NewCourse.Id,
      TeacherId = NewTeacher.Id,
      PhysicalRoomId = NewRoom.Id,
      StartDate = NewStartDate,
      Days = SelectedDays(),
      StartTime = start,
      EndTime = end,
      MinStudents = NewMinStudents,
      MaxStudents = NewMaxStudents
    };
    if (EditId == 0)
    {
      var created = await _classes.CreateAsync(input);
      var last = created.Lessons.Max(s => s.Date);
      Dialog.ShowInfo($"Đã Mở Lớp {created.Code}: {created.Lessons.Count} Buổi, Kết Thúc Dự Kiến {last:dd/MM/yyyy}");
    }
    else
    {
      var updated = await _classes.UpdateAsync(input);
      Dialog.ShowInfo($"Đã Cập Nhật Lớp {updated.Code}");
    }
    IsEditing = false;
    await LoadClassesAsync();
  });
  [RelayCommand(CanExecute = nameof(CanStartClass))]
  private Task StartClassAsync() => RunAsync(async () =>
  {
    var cls = RequireClass();
    if (!Dialog.Confirm($"Khai Giảng Lớp {cls.Code}? Cần Đủ {cls.MinStudents} Học Viên")) return;
    await _classes.StartAsync(cls.Id);
    Dialog.ShowInfo($"Lớp {cls.Code} Đã Chuyển Sang Đang Học");
    await LoadClassesAsync();
  });
  [RelayCommand(CanExecute = nameof(CanFinishClass))]
  private Task FinishClassAsync() => RunAsync(async () =>
  {
    var cls = RequireClass();
    if (!Dialog.Confirm($"Kết Thúc Lớp {cls.Code} & Xét Kết Quả Cho Học Viên?")) return;
    await _classes.FinishAsync(cls.Id);
    Dialog.ShowInfo($"Đã Kết Thúc Lớp {cls.Code} · Xem Kết Quả Ở Màn Hình Bảng Điểm");
    await LoadClassesAsync();
  });
  [RelayCommand(CanExecute = nameof(CanCancelClass))]
  private Task CancelClassAsync() => RunAsync(async () =>
  {
    var cls = RequireClass();
    var reason = Dialog.Prompt($"Hủy Lớp {cls.Code} · Lý Do Hủy:", "Lý Do Bắt Buộc");
    if (reason == null) return;
    await _classes.CancelAsync(cls.Id, reason);
    Dialog.ShowInfo($"Đã Hủy Lớp {cls.Code}");
    await LoadClassesAsync();
  });
  [RelayCommand(CanExecute = nameof(CanDeleteClass))]
  private Task DeleteClassAsync(ClassRoom? target = null) => RunAsync(async () =>
  {
    var cls = target ?? RequireClass();
    if (cls.Status is ClassStatus.InProgress or ClassStatus.Finished)
      throw new BusinessException($"Lớp {cls.Code} Đang Ở Trạng Thái {cls.Status.ToText()}, Không Thể Xóa · Hãy Dùng Chức Năng Hủy Lớp");
    if (cls.Enrollments.Any() || cls.ActiveStudentCount > 0)
      throw new BusinessException($"Lớp {cls.Code} Đã Có Học Viên Tham Gia, Không Thể Xóa · Hãy Dùng Chức Năng Hủy Lớp");
    if (!Dialog.Confirm($"Xác Nhận Xóa Vĩnh Viễn Lớp {cls.Code}?")) return;
    await _classes.DeleteAsync(cls.Id);
    Dialog.ShowInfo($"Đã Xóa Lớp {cls.Code}");
    if (EditId == cls.Id) IsEditing = false;
    await LoadClassesAsync();
  });
  [RelayCommand(CanExecute = nameof(CanCancelLesson))]
  private Task CancelLessonAsync() => RunAsync(async () =>
  {
    if (SelectedLesson == null) throw new BusinessException("Hãy Chọn 1 Buổi Học Trong Lịch");
    var reason = Dialog.Prompt($"Cho Nghỉ Buổi {SelectedLesson.LessonNo} Ngày {SelectedLesson.Date:dd/MM/yyyy} · Lý Do:", "Lý Do Bắt Buộc");
    if (reason == null) return;
    await _classes.CancelLessonAsync(SelectedLesson.Id, reason);
    await LoadLessonsAsync();
  });
  private ClassRoom RequireClass() => SelectedClass ?? throw new BusinessException("Hãy Chọn 1 Lớp");
}