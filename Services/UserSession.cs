using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
namespace EduFlow.Services.Common
{
  public class UserSession
  {
    public User? CurrentUser { get; private set; }
    public bool IsLoggedIn => CurrentUser != null;
    public void SignIn(User user) => CurrentUser = user;
    public void SignOut() => CurrentUser = null;
    public User RequireUser() =>
      CurrentUser ?? throw new BusinessException("Bạn Chưa Đăng Nhập");
    public bool Can(Permission permission) => CurrentUser?.Can(permission) == true;
    public User Require(Permission permission)
    {
      var user = RequireUser();
      if (!user.Can(permission))
        throw new BusinessException("Bạn Không Có Quyền Thực Hiện Chức Năng Này");
      return user;
    }
  }
}