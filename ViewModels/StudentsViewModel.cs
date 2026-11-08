using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services;
using EduFlow.Services.Common;
namespace EduFlow.WPF.ViewModels;
public partial class StudentsViewModel : ViewModelBase
{
  private readonly IStudentService _students;
  public ObservableCollection<Student> Students { get; } = new();
  [ObservableProperty] private string? _keyword;
  [ObservableProperty] private Student? _selectedStudent;
  [ObservableProperty] private bool _isEditing;
  [ObservableProperty] private int _editId;
  [ObservableProperty] private string _fullName = "";
  [ObservableProperty] private DateTime? _dateOfBirth;
  [ObservableProperty] private bool _isMale = true;
  [ObservableProperty] private string _phone = "";
  [ObservableProperty] private string? _email;
  [ObservableProperty] private string? _address;
  [ObservableProperty] private string? _note;
  public bool CanEdit { get; }
  public string FormTitle => EditId == 0 ? "Thêm Học Viên Mới" : "Cập Nhật Học Viên";
  public override string Title => "Quản Lý Học Viên";
  public StudentsViewModel(IStudentService students, UserSession session, IDialogService dialog) : base(dialog)
  {
    _students = students;
    CanEdit = session.Can(Permission.ManageStudents);
  }
  public override Task LoadAsync() => RunAsync(LoadStudentsAsync);
  private async Task LoadStudentsAsync()
  {
    Students.Clear();
    foreach (var s in await _students.SearchAsync(Keyword)) Students.Add(s);
  }
  partial void OnKeywordChanged(string? value) => _ = LoadStudentsAsync();
  partial void OnEditIdChanged(int value) => OnPropertyChanged(nameof(FormTitle));
  [RelayCommand]
  private void New()
  {
    SelectedStudent = null;
    EditId = 0;
    FullName = "";
    DateOfBirth = null;
    IsMale = true;
    Phone = "";
    Email = null;
    Address = null;
    Note = null;
    IsEditing = true;
  }
  [RelayCommand]
  private void Edit(Student? target = null)
  {
    var s = target ?? SelectedStudent;
    if (s == null) return;
    SelectedStudent = s;
    EditId = s.Id;
    FullName = s.FullName;
    DateOfBirth = s.DateOfBirth;
    IsMale = s.IsMale;
    Phone = s.Phone;
    Email = s.Email;
    Address = s.Address;
    Note = s.Note;
    IsEditing = true;
  }
  [RelayCommand]
  private void CancelEdit()
  {
    IsEditing = false;
  }
  [RelayCommand]
  private Task SaveAsync() => RunAsync(async () =>
  {
    var student = new Student
    {
      Id = EditId, FullName = FullName, DateOfBirth = DateOfBirth, IsMale = IsMale,
      Phone = Phone, Email = Email, Address = Address, Note = Note
    };
    var saved = await _students.SaveAsync(student);
    Dialog.ShowInfo($"Đã Lưu Học Viên {saved.Code} - {saved.FullName}");
    IsEditing = false;
    await LoadStudentsAsync();
  });
  [RelayCommand]
  private Task ToggleActiveAsync(Student? target = null) => RunAsync(async () =>
  {
    var s = target ?? SelectedStudent;
    if (s == null) return;
    bool newState = !s.IsActive;
    string action = newState ? "Kích Hoạt Lại" : "Ngừng Hoạt Động";
    if (!Dialog.Confirm($"Xác Nhận {action} Học Viên {s.FullName}?")) return;
    await _students.SetActiveAsync(s.Id, newState);
    await LoadStudentsAsync();
  });
  [RelayCommand]
  private Task DeleteAsync(Student? target = null) => RunAsync(async () =>
  {
    var s = target ?? SelectedStudent;
    if (s == null) return;
    if (!Dialog.Confirm($"Xác Nhận Xóa Vĩnh Viễn Học Viên {s.Code} - {s.FullName}?")) return;
    await _students.DeleteAsync(s.Id);
    Dialog.ShowInfo($"Đã Xóa Học Viên {s.FullName}");
    if (EditId == s.Id) IsEditing = false;
    await LoadStudentsAsync();
  });
}