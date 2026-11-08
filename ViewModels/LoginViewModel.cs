using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Models.Common;
using EduFlow.Services;
namespace EduFlow.WPF.ViewModels;
public partial class LoginViewModel : ViewModelBase
{
  private readonly IAuthService _auth;
  public event Action? LoginSucceeded;
  [ObservableProperty]
  private string _username = "";
  [ObservableProperty]
  private string? _errorMessage;
  public override string Title => "Đăng Nhập";
  public LoginViewModel(IAuthService auth, IDialogService dialog) : base(dialog) => _auth = auth;
  [RelayCommand]
  private async Task LoginAsync(string password)
  {
    if (IsBusy) return;
    ErrorMessage = null;
    IsBusy = true;
    try
    {
      await _auth.LoginAsync(Username, password);
      LoginSucceeded?.Invoke();
    }
    catch (BusinessException ex)
    {
      ErrorMessage = ex.Message;
    }
    catch (Exception ex)
    {
      ErrorMessage = "Không Thể Kết Nối CSDL: " + ex.Message;
    }
    finally
    {
      IsBusy = false;
    }
  }
}