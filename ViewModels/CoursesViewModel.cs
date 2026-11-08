using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services;
using EduFlow.Services.Common;
namespace EduFlow.WPF.ViewModels;
public partial class CoursesViewModel : ViewModelBase
{
  private readonly ICourseService _courses;
  private readonly ICatalogService _catalog;
  public ObservableCollection<Course> Courses { get; } = new();
  public ObservableCollection<Room> Rooms { get; } = new();
  public CourseCategory[] Categories { get; } = Enum.GetValues(typeof(CourseCategory)).Cast<CourseCategory>().ToArray();
  public bool CanEdit { get; }
  [ObservableProperty] private Course? _selectedCourse;
  [ObservableProperty] private bool _isEditing;
  [ObservableProperty] private int _editId;
  [ObservableProperty] private string _code = "";
  [ObservableProperty] private string _name = "";
  [ObservableProperty] private CourseCategory _category = CourseCategory.Language;
  [ObservableProperty] private int _sessionCount = 24;
  [ObservableProperty] private decimal _tuitionFee = 3_000_000;
  [ObservableProperty] private decimal _monthlyFee = 1_000_000;
  [ObservableProperty] private string? _description;
  [ObservableProperty] private bool _isActive = true;
  [ObservableProperty] private Room? _selectedRoom;
  [ObservableProperty] private bool _isEditingRoom;
  [ObservableProperty] private int _roomEditId;
  [ObservableProperty] private string _roomName = "";
  [ObservableProperty] private int _roomCapacity = 20;
  [ObservableProperty] private bool _roomIsActive = true;
  public string FormTitle => EditId == 0 ? "Thêm Khóa Học Mới" : "Cập Nhật Khóa Học";
  public string RoomFormTitle => RoomEditId == 0 ? "Thêm Phòng Học Mới" : "Cập Nhật Phòng Học";
  public override string Title => "Khóa Học & Phòng Học";
  public CoursesViewModel(ICourseService courses, ICatalogService catalog, UserSession session, IDialogService dialog) : base(dialog)
  {
    _courses = courses;
    _catalog = catalog;
    CanEdit = session.Can(Permission.ManageCatalog);
  }
  public override Task LoadAsync() => RunAsync(LoadAllAsync);
  private async Task LoadAllAsync()
  {
    Courses.Clear();
    foreach (var c in await _courses.GetAllAsync()) Courses.Add(c);
    Rooms.Clear();
    foreach (var r in await _catalog.GetRoomsAsync(activeOnly: false)) Rooms.Add(r);
  }
  partial void OnEditIdChanged(int value) => OnPropertyChanged(nameof(FormTitle));
  partial void OnRoomEditIdChanged(int value) => OnPropertyChanged(nameof(RoomFormTitle));
  [RelayCommand]
  private void New()
  {
    SelectedCourse = null;
    EditId = 0;
    Code = "";
    Name = "";
    Category = CourseCategory.Language;
    SessionCount = 24;
    TuitionFee = 3_000_000;
    MonthlyFee = 1_000_000;
    Description = null;
    IsActive = true;
    IsEditing = true;
    IsEditingRoom = false;
  }
  [RelayCommand]
  private void Edit(Course? target = null)
  {
    var c = target ?? SelectedCourse;
    if (c == null) return;
    SelectedCourse = c;
    EditId = c.Id;
    Code = c.Code;
    Name = c.Name;
    Category = c.Category;
    SessionCount = c.SessionCount;
    TuitionFee = c.TuitionFee;
    MonthlyFee = c.MonthlyFee;
    Description = c.Description;
    IsActive = c.IsActive;
    IsEditing = true;
    IsEditingRoom = false;
  }
  [RelayCommand]
  private void CancelEdit()
  {
    IsEditing = false;
  }
  [RelayCommand]
  private Task SaveAsync() => RunAsync(async () =>
  {
    await _courses.SaveAsync(new Course
    {
      Id = EditId, Code = Code, Name = Name, Category = Category,
      SessionCount = SessionCount, TuitionFee = TuitionFee, MonthlyFee = MonthlyFee, Description = Description, IsActive = IsActive
    });
    Dialog.ShowInfo("Đã Lưu Khóa Học");
    IsEditing = false;
    await LoadAllAsync();
  });
  [RelayCommand]
  private Task DeleteAsync(Course? target = null) => RunAsync(async () =>
  {
    var c = target ?? SelectedCourse;
    if (c == null) return;
    if (!Dialog.Confirm($"Xác Nhận Xóa Vĩnh Viễn Khóa Học {c.Code} - {c.Name}?")) return;
    await _courses.DeleteAsync(c.Id);
    Dialog.ShowInfo($"Đã Xóa Khóa Học {c.Code}");
    if (EditId == c.Id) IsEditing = false;
    await LoadAllAsync();
  });
  [RelayCommand]
  private void NewRoom()
  {
    SelectedRoom = null;
    RoomEditId = 0;
    RoomName = "";
    RoomCapacity = 20;
    RoomIsActive = true;
    IsEditingRoom = true;
    IsEditing = false;
  }
  [RelayCommand]
  private void EditRoom(Room? target = null)
  {
    var r = target ?? SelectedRoom;
    if (r == null) return;
    SelectedRoom = r;
    RoomEditId = r.Id;
    RoomName = r.Name;
    RoomCapacity = r.Capacity;
    RoomIsActive = r.IsActive;
    IsEditingRoom = true;
    IsEditing = false;
  }
  [RelayCommand]
  private void CancelEditRoom()
  {
    IsEditingRoom = false;
  }
  [RelayCommand]
  private Task SaveRoomAsync() => RunAsync(async () =>
  {
    await _catalog.SaveRoomAsync(new Room
    {
      Id = RoomEditId, Name = RoomName, Capacity = RoomCapacity, IsActive = RoomIsActive
    });
    Dialog.ShowInfo("Đã Lưu Phòng Học");
    IsEditingRoom = false;
    await LoadAllAsync();
  });
  [RelayCommand]
  private Task DeleteRoomAsync(Room? target = null) => RunAsync(async () =>
  {
    var r = target ?? SelectedRoom;
    if (r == null) return;
    if (!Dialog.Confirm($"Xác Nhận Xóa Vĩnh Viễn Phòng Học {r.Name}?")) return;
    await _catalog.DeleteRoomAsync(r.Id);
    Dialog.ShowInfo($"Đã Xóa Phòng Học {r.Name}");
    if (RoomEditId == r.Id) IsEditingRoom = false;
    await LoadAllAsync();
  });
}