using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
namespace EduFlow.Services;
public interface IUserService
{
  Task<List<User>> GetAllAsync();
  Task<List<User>> GetTeachersAsync(bool activeOnly = true);
  Task<User> SaveAsync(User user, string? newPassword);
  Task SetActiveAsync(int userId, bool isActive);
  Task DeleteAsync(int userId);
}
public class UserService : BaseService, IUserService
{
  public UserService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
  public async Task<List<User>> GetAllAsync()
  {
    var current = Session.RequireUser();
    if (current.Role != UserRole.Admin)
      throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Tài Khoản");
    using var db = CreateContext();
    return await db.Users.AsNoTracking()
      .Where(u => u.Role != UserRole.Admin)
      .OrderBy(u => u.Role)
      .ThenBy(u => u.FullName)
      .ToListAsync();
  }
  public async Task<List<User>> GetTeachersAsync(bool activeOnly = true)
  {
    Session.RequireUser();
    using var db = CreateContext();
    var q = db.Users.AsNoTracking().Where(u => u.Role == UserRole.Teacher);
    if (activeOnly) q = q.Where(u => u.IsActive);
    return await q.OrderBy(u => u.FullName).ToListAsync();
  }
  public async Task<User> SaveAsync(User input, string? newPassword)
  {
    var current = Session.RequireUser();
    if (current.Role != UserRole.Admin)
      throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Tài Khoản");
    if (string.IsNullOrWhiteSpace(input.Username))
      throw new BusinessException("Tên Đăng Nhập Không Được Để Trống");
    if (string.IsNullOrWhiteSpace(input.FullName))
      throw new BusinessException("Họ & Tên Không Được Để Trống");
    if (string.IsNullOrWhiteSpace(input.Phone))
      throw new BusinessException("Số Điện Thoại Không Được Để Trống");
    if (!PhoneValidator.IsValid(input.Phone))
      throw new BusinessException("Số Điện Thoại Không Hợp Lệ (10 Số, Bắt Đầu Bằng 0)");
    if (string.IsNullOrWhiteSpace(input.Email))
      throw new BusinessException("Email Không Được Để Trống");
    if (!input.Email!.Contains("@") || !input.Email.Contains("."))
      throw new BusinessException("Email Không Hợp Lệ");
    if (input.Role is not (UserRole.Staff or UserRole.Teacher))
      throw new BusinessException("Vai Trò Không Hợp Lệ");
    if (input.Id == 0 && string.IsNullOrWhiteSpace(newPassword))
      throw new BusinessException("Mật Khẩu Không Được Để Trống");
    if (!string.IsNullOrWhiteSpace(newPassword) && newPassword!.Length < 6)
      throw new BusinessException("Mật Khẩu Phải Có Ít Nhất 6 Ký Tự");
    using var db = CreateContext();
    string username = input.Username.Trim().ToLower();
    if (await db.Users.AnyAsync(u => u.Username == username && u.Id != input.Id))
      throw new BusinessException($"Tên Đăng Nhập {username} Đã Tồn Tại");
    User user;
    if (input.Id == 0)
    {
      user = new User { IsActive = true };
      db.Users.Add(user);
    }
    else
    {
      user = await db.Users.FindAsync(input.Id) ?? throw new BusinessException("Không Tìm Thấy Tài Khoản");
      if (user.Role == UserRole.Admin)
        throw new BusinessException("Không Thể Sửa Tài Khoản Quản Trị Hệ Thống");
      if (user.Role != input.Role && user.Role == UserRole.Teacher && await db.Classes.AnyAsync(c => c.TeacherId == user.Id
          && (c.Status == ClassStatus.Open || c.Status == ClassStatus.InProgress)))
        throw new BusinessException("Giáo Viên Đang Phụ Trách Lớp Chưa Kết Thúc, Không Thể Đổi Vai Trò");
    }
    user.Username = username;
    user.FullName = input.FullName.Trim();
    user.Phone = input.Phone!.Trim();
    user.Email = input.Email!.Trim();
    user.Role = input.Role;
    if (!string.IsNullOrWhiteSpace(newPassword))
      user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
    await db.SaveChangesAsync();
    return user;
  }
  public async Task SetActiveAsync(int userId, bool isActive)
  {
    var me = Session.RequireUser();
    if (me.Role != UserRole.Admin)
      throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Tài Khoản");
    if (userId == me.Id && !isActive)
      throw new BusinessException("Bạn Không Thể Tự Khóa Tài Khoản Của Mình");
    using var db = CreateContext();
    var user = await db.Users.FindAsync(userId) ?? throw new BusinessException("Không Tìm Thấy Tài Khoản");
    if (user.Role == UserRole.Admin)
      throw new BusinessException("Không Thể Khóa Tài Khoản Quản Trị Hệ Thống");
    user.IsActive = isActive;
    await db.SaveChangesAsync();
  }
  public async Task DeleteAsync(int userId)
  {
    var me = Session.RequireUser();
    if (me.Role != UserRole.Admin)
      throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Tài Khoản");
    if (userId == me.Id)
      throw new BusinessException("Bạn Không Thể Tự Xóa Tài Khoản Của Mình");
    using var db = CreateContext();
    var user = await db.Users.FindAsync(userId) ?? throw new BusinessException("Không Tìm Thấy Tài Khoản");
    if (user.Role == UserRole.Admin)
      throw new BusinessException("Không Thể Xóa Tài Khoản Quản Trị Hệ Thống");
    if (await db.Classes.AnyAsync(c => c.TeacherId == userId && (c.Status == ClassStatus.Open || c.Status == ClassStatus.InProgress)))
      throw new BusinessException("Giáo Viên Đang Phụ Trách Lớp Chưa Kết Thúc · Hãy Đổi Giáo Viên Ở Màn Hình Lớp Học Trước Khi Xóa");
    var classes = await db.Classes.Where(c => c.TeacherId == userId).ToListAsync();
    foreach (var c in classes) c.TeacherId = me.Id;
    var lessons = await db.Lessons.Where(l => l.TeacherId == userId).ToListAsync();
    foreach (var l in lessons) l.TeacherId = me.Id;
    db.Users.Remove(user);
    await db.SaveChangesAsync();
  }
}