using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
namespace EduFlow.Models.Enums
{
  public enum UserRole
  {
    [Description("Quản Trị Viên")] Admin = 0,
    [Description("Nhân Viên")] Staff = 1,
    [Description("Giáo Viên")] Teacher = 2
  }
  public enum CourseCategory
  {
    [Description("Ngoại Ngữ")] Language = 1,
    [Description("Tin Học")] Computer = 2,
    [Description("Ngắn Hạn")] ShortCourse = 3
  }
  public enum LeadStatus
  {
    [Description("Mới")] New = 1,
    [Description("Đã Liên Hệ")] Contacted = 2,
    [Description("Hẹn Học Thử")] TrialScheduled = 3,
    [Description("Đã Học Thử")] TrialDone = 4,
    [Description("Từ Chối")] Rejected = 5,
    [Description("Đã Chuyển Học Viên")] Converted = 6
  }
  public enum ClassStatus
  {
    [Description("Đang Tuyển Sinh")] Open = 1,
    [Description("Đang Học")] InProgress = 2,
    [Description("Đã Kết Thúc")] Finished = 3,
    [Description("Đã Hủy")] Cancelled = 4
  }
  public enum LessonStatus
  {
    [Description("Đã Lên Lịch")] Scheduled = 1,
    [Description("Đã Dạy")] Done = 2,
    [Description("Đã Hủy")] Cancelled = 3
  }
  public enum EnrollmentStatus
  {
    [Description("Đang Học")] Active = 1,
    [Description("Bảo Lưu")] Reserved = 2,
    [Description("Đã Nghỉ")] Dropped = 3,
    [Description("Hoàn Thành")] Completed = 4,
    [Description("Không Đạt")] Failed = 5
  }
  public enum PaymentStatus
  {
    [Description("Chưa Đóng")] Unpaid = 1,
    [Description("Đóng 1 Phần")] Partial = 2,
    [Description("Đã Đóng Đủ")] Paid = 3
  }
  public enum PaymentMethod
  {
    [Description("Tiền Mặt")] Cash = 1,
    [Description("Chuyển Khoản")] BankTransfer = 2
  }
  public enum FeeType
  {
    [Description("Theo Tháng")] Monthly = 1,
    [Description("Trọn Gói")] OneTime = 2
  }
  public enum ReceiptStatus
  {
    [Description("Hiệu Lực")] Valid = 1,
    [Description("Đã Hủy")] Cancelled = 2
  }
  public enum ReductionStatus
  {
    [Description("Chờ Duyệt")] Pending = 1,
    [Description("Đã Duyệt")] Approved = 2,
    [Description("Từ Chối")] Rejected = 3
  }
  public enum AttendanceStatus
  {
    [Description("Có Mặt")] Present = 1,
    [Description("Đi Muộn")] Late = 2,
    [Description("Vắng Có Phép")] AbsentExcused = 3,
    [Description("Vắng Không Phép")] AbsentUnexcused = 4
  }
  public enum ReminderStatus
  {
    [Description("Chưa Xử Lý")] Open = 1,
    [Description("Đã Xử Lý")] Done = 2,
    [Description("Bỏ Qua")] Skipped = 3
  }
  [Flags]
  public enum WeekDays
  {
    None = 0,
    [Description("T2")] Monday = 1,
    [Description("T3")] Tuesday = 2,
    [Description("T4")] Wednesday = 4,
    [Description("T5")] Thursday = 8,
    [Description("T6")] Friday = 16,
    [Description("T7")] Saturday = 32,
    [Description("CN")] Sunday = 64
  }
  public static class EnumExtensions
  {
    public static string ToText(this Enum value)
    {
      FieldInfo? field = value.GetType().GetField(value.ToString());
      var attr = field?.GetCustomAttribute<DescriptionAttribute>();
      return attr?.Description ?? value.ToString();
    }
    public static WeekDays ToWeekDay(this DayOfWeek day) => day switch
    {
      DayOfWeek.Monday => WeekDays.Monday,
      DayOfWeek.Tuesday => WeekDays.Tuesday,
      DayOfWeek.Wednesday => WeekDays.Wednesday,
      DayOfWeek.Thursday => WeekDays.Thursday,
      DayOfWeek.Friday => WeekDays.Friday,
      DayOfWeek.Saturday => WeekDays.Saturday,
      _ => WeekDays.Sunday
    };
    public static string ToShortText(this WeekDays days) =>
      string.Join(", ", Enum.GetValues(typeof(WeekDays)).Cast<WeekDays>().ToArray()
        .Where(d => d != WeekDays.None && days.HasFlag(d))
        .Select(d => d.ToText()));
  }
  public enum Permission
  {
    ManageUsers,
    ManageSettings,
    ManageLeads,
    ManageStudents,
    ManageCatalog,
    ManageClasses,
    CancelClass,
    Enroll,
    TakeAttendance,
    EnterScores,
    CollectFees,
    CancelReceipt,
    RequestFeeReduction,
    ApproveFeeReduction,
    ViewFinanceReport
  }
  public static class RolePermissions
  {
    private static readonly Dictionary<UserRole, HashSet<Permission>> Matrix = new()
    {
      [UserRole.Admin] = Enum.GetValues(typeof(Permission)).Cast<Permission>().ToArray().ToHashSet(),
      [UserRole.Staff] = new()
      {
        Permission.ManageStudents, Permission.ManageCatalog, Permission.ManageClasses,
        Permission.Enroll, Permission.CollectFees, Permission.RequestFeeReduction
      },
      [UserRole.Teacher] = new()
      {
        Permission.TakeAttendance, Permission.EnterScores
      }
    };
    public static bool Has(UserRole role, Permission permission) =>
      role == UserRole.Admin || (Matrix.TryGetValue(role, out var set) && set.Contains(permission));
  }
}