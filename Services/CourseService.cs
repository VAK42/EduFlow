using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
namespace EduFlow.Services;
public interface ICourseService
{
  Task<List<Course>> GetAllAsync(bool activeOnly = false);
  Task<Course> SaveAsync(Course course);
  Task DeleteAsync(int courseId);
}
public class CourseService : BaseService, ICourseService
{
  public CourseService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
  public async Task<List<Course>> GetAllAsync(bool activeOnly = false)
  {
    using var db = CreateContext();
    var query = db.Courses.AsNoTracking().AsQueryable();
    if (activeOnly) query = query.Where(c => c.IsActive);
    return await query.OrderBy(c => c.Category).ThenBy(c => c.Code).ToListAsync();
  }
  public async Task<Course> SaveAsync(Course input)
  {
    Session.Require(Permission.ManageCatalog);
    if (string.IsNullOrWhiteSpace(input.Code))
      throw new BusinessException("Mã Khóa Học Không Được Để Trống");
    if (string.IsNullOrWhiteSpace(input.Name))
      throw new BusinessException("Tên Khóa Học Không Được Để Trống");
    if (!Enum.IsDefined(typeof(CourseCategory), input.Category))
      throw new BusinessException("Phải Chọn Nhóm Khóa Học");
    if (input.SessionCount <= 0 || input.SessionCount > 200)
      throw new BusinessException("Số Buổi Phải Từ 1 Đến 200");
    if (input.MonthlyFee <= 0)
      throw new BusinessException("Học Phí Theo Tháng Phải Lớn Hơn 0");
    if (input.TuitionFee <= 0)
      throw new BusinessException("Học Phí Trọn Gói Phải Lớn Hơn 0");
    using var db = CreateContext();
    string code = input.Code.Trim().ToUpper();
    if (await db.Courses.AnyAsync(c => c.Code == code && c.Id != input.Id))
      throw new BusinessException($"Mã Khóa Học {code} Đã Tồn Tại");
    Course course;
    if (input.Id == 0)
    {
      course = new Course();
      db.Courses.Add(course);
    }
    else
    {
      course = await db.Courses.FindAsync(input.Id) ?? throw new BusinessException("Không Tìm Thấy Khóa Học");
      bool hasActiveClasses = await db.Classes.AnyAsync(c => c.CourseId == course.Id && c.Status != ClassStatus.Cancelled);
      if (hasActiveClasses && course.SessionCount != input.SessionCount)
        throw new BusinessException("Khóa Học Đã Mở Lớp, Không Thể Đổi Số Buổi");
      if (!input.IsActive && await db.Classes.AnyAsync(c => c.CourseId == course.Id && c.Status == ClassStatus.Open))
        throw new BusinessException("Khóa Học Đang Có Lớp Tuyển Sinh, Không Thể Ngừng Mở Lớp");
    }
    course.Code = code;
    course.Name = input.Name.Trim();
    course.Category = input.Category;
    course.SessionCount = input.SessionCount;
    course.TuitionFee = input.TuitionFee;
    course.MonthlyFee = input.MonthlyFee;
    course.Description = input.Description?.Trim();
    course.IsActive = input.IsActive;
    await db.SaveChangesAsync();
    return course;
  }
  public async Task DeleteAsync(int courseId)
  {
    Session.Require(Permission.ManageCatalog);
    using var db = CreateContext();
    var course = await db.Courses.FindAsync(courseId)
      ?? throw new BusinessException("Không Tìm Thấy Khóa Học");
    if (await db.Classes.AnyAsync(c => c.CourseId == courseId && c.Status != ClassStatus.Cancelled))
      throw new BusinessException("Khóa Học Đã Mở Lớp, Không Thể Xóa · Hãy Bỏ Chọn Đang Mở Lớp Để Ngừng Tuyển Sinh");
    var cancelledClasses = await db.Classes.Include(c => c.Lessons).Include(c => c.Enrollments).Where(c => c.CourseId == courseId).ToListAsync();
    if (cancelledClasses.Any(c => c.Enrollments.Any()))
      throw new BusinessException("Khóa Học Đã Có Lịch Sử Ghi Danh, Không Thể Xóa");
    foreach (var c in cancelledClasses) db.Lessons.RemoveRange(c.Lessons);
    db.Classes.RemoveRange(cancelledClasses);
    db.Courses.Remove(course);
    await db.SaveChangesAsync();
  }
}