using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
namespace EduFlow.Services;
public interface IStudentService
{
  Task<List<Student>> SearchAsync(string? keyword);
  Task<Student> SaveAsync(Student student);
  Task SetActiveAsync(int studentId, bool isActive);
  Task DeleteAsync(int studentId);
}
public class StudentService : BaseService, IStudentService
{
  public StudentService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
  public async Task<List<Student>> SearchAsync(string? keyword)
  {
    var user = Session.RequireUser();
    using var db = CreateContext();
    var query = db.Students.AsNoTracking().AsQueryable();
    if (user.IsTeacher)
      query = query.Where(s => s.Enrollments.Any(e => e.ClassRoom!.TeacherId == user.Id));
    if (!string.IsNullOrWhiteSpace(keyword))
    {
      keyword = keyword!.Trim();
      query = query.Where(s => s.FullName.Contains(keyword) || s.Code.Contains(keyword) || s.Phone.Contains(keyword));
    }
    return await query.OrderBy(s => s.Code).ToListAsync();
  }
  public async Task<Student> SaveAsync(Student input)
  {
    Session.Require(Permission.ManageStudents);
    if (string.IsNullOrWhiteSpace(input.FullName))
      throw new BusinessException("Họ Tên Học Viên Không Được Để Trống");
    if (!input.DateOfBirth.HasValue)
      throw new BusinessException("Vui Lòng Chọn Ngày Sinh");
    if (input.DateOfBirth.Value.Date > DateTime.Today)
      throw new BusinessException("Ngày Sinh Không Được Ở Tương Lai");
    if (string.IsNullOrWhiteSpace(input.Phone))
      throw new BusinessException("Số Điện Thoại Không Được Để Trống");
    if (!PhoneValidator.IsValid(input.Phone))
      throw new BusinessException("Số Điện Thoại Không Hợp Lệ · 10 Số Và Bắt Đầu Bằng 0");
    if (string.IsNullOrWhiteSpace(input.Email))
      throw new BusinessException("Email Không Được Để Trống");
    if (string.IsNullOrWhiteSpace(input.Address))
      throw new BusinessException("Địa Chỉ Không Được Để Trống");
    using var db = CreateContext();
    string phone = input.Phone.Trim();
    if (await db.Students.AnyAsync(s => s.Phone == phone && s.Id != input.Id))
      throw new BusinessException($"Số Điện Thoại {phone} Đã Được Dùng Cho Học Viên Khác");
    bool isNew = input.Id == 0;
    Student student;
    if (isNew)
    {
      student = new Student { Code = "TMP" + Guid.NewGuid().ToString("N").Substring(0, 7) };
      db.Students.Add(student);
    }
    else
    {
      student = await db.Students.FirstOrDefaultAsync(s => s.Id == input.Id)
        ?? throw new BusinessException("Không Tìm Thấy Học Viên");
    }
    student.FullName = input.FullName.Trim();
    student.Phone = input.Phone.Trim();
    student.Email = input.Email?.Trim();
    student.DateOfBirth = input.DateOfBirth;
    student.IsMale = input.IsMale;
    student.Address = input.Address?.Trim();
    student.Note = input.Note?.Trim();
    using var tx = db.Database.BeginTransaction();
    await db.SaveChangesAsync();
    if (isNew) student.Code = $"{DateTime.Today.Year}{student.Id:D4}";
    Audit(db, isNew ? "Student.Create" : "Student.Update", "Student", student.Id, student.Code);
    await db.SaveChangesAsync();
    tx.Commit();
    return student;
  }
  public async Task SetActiveAsync(int studentId, bool isActive)
  {
    Session.Require(Permission.ManageStudents);
    using var db = CreateContext();
    var student = await db.Students.FindAsync(studentId)
      ?? throw new BusinessException("Không Tìm Thấy Học Viên");
    if (!isActive && await db.Enrollments.AnyAsync(e => e.StudentId == studentId && e.Status == EnrollmentStatus.Active))
      throw new BusinessException("Học Viên Đang Theo Học 1 Lớp, Hãy Cho Nghỉ Học Ở Lớp Đó Trước");
    student.IsActive = isActive;
    Audit(db, isActive ? "Student.Activate" : "Student.Deactivate", "Student", student.Id);
    await db.SaveChangesAsync();
  }
  public async Task DeleteAsync(int studentId)
  {
    Session.Require(Permission.ManageStudents);
    using var db = CreateContext();
    var student = await db.Students.FindAsync(studentId)
      ?? throw new BusinessException("Không Tìm Thấy Học Viên");
    if (await db.Enrollments.AnyAsync(e => e.StudentId == studentId && e.Status != EnrollmentStatus.Dropped)
      || await db.TuitionFees.AnyAsync(f => f.StudentId == studentId && f.IsPaid))
      throw new BusinessException("Học Viên Đang Học Hoặc Đã Có Lịch Sử Thu Học Phí, Không Thể Xóa · Hãy Dùng Chức Năng Ngừng Hoạt Động");
    var dropped = await db.Enrollments
      .Include(e => e.Attendances)
      .Where(e => e.StudentId == studentId)
      .ToListAsync();
    foreach (var e in dropped)
    {
      db.Attendances.RemoveRange(e.Attendances);
    }
    db.Enrollments.RemoveRange(dropped);
    var fees = await db.TuitionFees.Where(f => f.StudentId == studentId).ToListAsync();
    db.TuitionFees.RemoveRange(fees);
    db.Students.Remove(student);
    Audit(db, "Student.Delete", "Student", student.Id, student.Code);
    await db.SaveChangesAsync();
  }
}
public static class PhoneValidator
{
  public static bool IsValid(string? phone) =>
    phone != null && phone.Trim().Length == 10 && phone.Trim().StartsWith("0") && phone.Trim().All(char.IsDigit);
}