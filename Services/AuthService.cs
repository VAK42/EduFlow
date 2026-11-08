using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
namespace EduFlow.Services;
public interface IAuthService
{
  Task<User> LoginAsync(string username, string password);
  void Logout();
  Task ChangePasswordAsync(string oldPassword, string newPassword);
}
public class AuthService : BaseService, IAuthService
{
  public AuthService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
  public async Task<User> LoginAsync(string username, string password)
  {
    if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
      throw new BusinessException("Vui Lòng Nhập Tên Đăng Nhập & Mật Khẩu");
    string cleanUsername = username.Trim().ToLower();
    using var db = CreateContext();
    var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Username == cleanUsername);
    if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
      throw new BusinessException("Tên Đăng Nhập Hoặc Mật Khẩu Không Đúng");
    if (!user.IsActive)
      throw new BusinessException("Tài Khoản Đã Bị Khóa · Liên Hệ Quản Lý");
    Session.SignIn(user);
    Audit(db, "User.Login", "User", user.Id);
    await db.SaveChangesAsync();
    return user;
  }
  public void Logout() => Session.SignOut();
  public async Task ChangePasswordAsync(string oldPassword, string newPassword)
  {
    var current = Session.RequireUser();
    if (newPassword.Length < 6)
      throw new BusinessException("Mật Khẩu Mới Phải Có Ít Nhất 6 Ký Tự");
    using var db = CreateContext();
    var user = await db.Users.FirstAsync(u => u.Id == current.Id);
    if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
      throw new BusinessException("Mật Khẩu Cũ Không Đúng");
    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
    Audit(db, "User.ChangePassword", "User", user.Id);
    await db.SaveChangesAsync();
  }
}