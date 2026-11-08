using EduFlow.Models.Common;
using EduFlow.Models.Enums;
namespace EduFlow.Models.Entities
{
  public class User : BaseEntity
  {
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public bool Can(Permission permission) => Role == UserRole.Admin || RolePermissions.Has(Role, permission);
    public bool IsAdmin => Role == UserRole.Admin;
    public bool IsManager => Role == UserRole.Admin;
    public bool IsTeacher => Role == UserRole.Teacher;
    public override string ToString() => FullName;
  }
}