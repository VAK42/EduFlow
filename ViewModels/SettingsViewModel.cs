using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Models.Common;
using EduFlow.Models.Enums;
using EduFlow.Services;
using EduFlow.Services.Common;
namespace EduFlow.WPF.ViewModels
{
  public partial class SettingsViewModel : ViewModelBase
  {
    private readonly UserSession _session;
    private readonly IAuthService _auth;
    [ObservableProperty] private string _username = "";
    [ObservableProperty] private string _fullName = "";
    [ObservableProperty] private UserRole _role = UserRole.Teacher;
    [ObservableProperty] private string _roleText = "";
    [ObservableProperty] private string _phone = "";
    [ObservableProperty] private string _email = "";
    [ObservableProperty] private string _oldPassword = "";
    [ObservableProperty] private string _newPassword = "";
    [ObservableProperty] private string _confirmPassword = "";
    [ObservableProperty] private bool _isChangePasswordOpen;
    public override string Title => "Cài Đặt";
    public SettingsViewModel(UserSession session, IAuthService auth, IDialogService dialog) : base(dialog)
    {
      _session = session;
      _auth = auth;
    }
    public override Task LoadAsync()
    {
      var user = _session.RequireUser();
      Username = user.Username;
      FullName = user.FullName;
      Role = user.Role;
      RoleText = user.Role.ToText();
      Phone = string.IsNullOrWhiteSpace(user.Phone) ? "Chưa Cập Nhật" : user.Phone!;
      Email = string.IsNullOrWhiteSpace(user.Email) ? "Chưa Cập Nhật" : user.Email!;
      OldPassword = "";
      NewPassword = "";
      ConfirmPassword = "";
      IsChangePasswordOpen = false;
      return Task.CompletedTask;
    }
    [RelayCommand]
    private void OpenChangePassword()
    {
      OldPassword = "";
      NewPassword = "";
      ConfirmPassword = "";
      IsChangePasswordOpen = true;
    }
    [RelayCommand]
    private void CloseChangePassword()
    {
      IsChangePasswordOpen = false;
      OldPassword = "";
      NewPassword = "";
      ConfirmPassword = "";
    }
    [RelayCommand]
    private Task ChangePasswordAsync() => RunAsync(async () =>
    {
      if (string.IsNullOrWhiteSpace(OldPassword))
        throw new BusinessException("Vui Lòng Nhập Mật Khẩu Hiện Tại");
      if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        throw new BusinessException("Mật Khẩu Mới Phải Có Ít Nhất 6 Ký Tự");
      if (NewPassword != ConfirmPassword)
        throw new BusinessException("Xác Nhận Mật Khẩu Mới Không Khớp");
      await _auth.ChangePasswordAsync(OldPassword, NewPassword);
      IsChangePasswordOpen = false;
      OldPassword = "";
      NewPassword = "";
      ConfirmPassword = "";
      Dialog.ShowInfo("Đã Đổi Mật Khẩu Thành Công");
    });
  }
}