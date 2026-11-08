using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services;
namespace EduFlow.WPF.ViewModels
{
  public partial class UsersViewModel : ViewModelBase
  {
    private readonly IUserService _users;
    public ObservableCollection<User> Users { get; } = new();
    public UserRole[] Roles { get; } = new[] { UserRole.Staff, UserRole.Teacher };
    [ObservableProperty] private User? _selectedUser;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private int _editId;
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private UserRole _role = UserRole.Teacher;
    [ObservableProperty] private string? _newPassword;
    public string FormTitle => EditId == 0 ? "Thêm Tài Khoản Mới" : "Chỉnh Sửa Tài Khoản";
    public string PasswordHint => EditId == 0 ? "Mật Khẩu *" : "Mật Khẩu (Để Trống Nếu Không Đổi)";
    public override string Title => "Tài Khoản Người Dùng";
    public UsersViewModel(IUserService users, IDialogService dialog) : base(dialog) => _users = users;
    public override Task LoadAsync() => RunAsync(LoadUsersAsync);
    private async Task LoadUsersAsync()
    {
      Users.Clear();
      foreach (var u in await _users.GetAllAsync()) Users.Add(u);
    }
    partial void OnEditIdChanged(int value)
    {
      OnPropertyChanged(nameof(FormTitle));
      OnPropertyChanged(nameof(PasswordHint));
    }
    [RelayCommand]
    private void New()
    {
      EditId = 0;
      Username = "";
      FullName = "";
      Phone = "";
      Email = "";
      Role = UserRole.Teacher;
      NewPassword = null;
      IsEditing = true;
    }
    [RelayCommand]
    private void Edit(User? target = null)
    {
      var u = target ?? SelectedUser;
      if (u == null) return;
      SelectedUser = u;
      EditId = u.Id;
      Username = u.Username;
      FullName = u.FullName;
      Phone = u.Phone ?? "";
      Email = u.Email ?? "";
      Role = u.Role;
      NewPassword = null;
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
      var saved = await _users.SaveAsync(new User
      {
        Id = EditId,
        Username = Username,
        FullName = FullName,
        Phone = Phone,
        Email = Email,
        Role = Role
      }, NewPassword);
      Dialog.ShowInfo($"Đã Lưu Tài Khoản {saved.Username}");
      IsEditing = false;
      await LoadUsersAsync();
    });
    [RelayCommand]
    private Task ToggleActiveAsync(User? target = null) => RunAsync(async () =>
    {
      var u = target ?? SelectedUser;
      if (u == null) return;
      bool newState = !u.IsActive;
      if (!Dialog.Confirm($"{(newState ? "Mở Khóa" : "Khóa")} Tài Khoản {u.Username}?")) return;
      await _users.SetActiveAsync(u.Id, newState);
      await LoadUsersAsync();
    });
    [RelayCommand]
    private Task DeleteAsync(User? target = null) => RunAsync(async () =>
    {
      var u = target ?? SelectedUser;
      if (u == null) return;
      if (!Dialog.Confirm($"Xác Nhận Xóa Vĩnh Viễn Tài Khoản {u.Username}?")) return;
      await _users.DeleteAsync(u.Id);
      Dialog.ShowInfo($"Đã Xóa Tài Khoản {u.Username}");
      if (EditId == u.Id) IsEditing = false;
      await LoadUsersAsync();
    });
  }
}