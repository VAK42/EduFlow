using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services.Common;
using EduFlow.Services.Dtos;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
namespace EduFlow.Services;
public interface IAttendanceService
{
  Task<List<AttendanceRow>> GetSheetAsync(int lessonId);
  Task SaveAsync(int lessonId, IEnumerable<AttendanceRow> rows, string? content);
}
public class AttendanceService : BaseService, IAttendanceService
{
  public AttendanceService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
  public async Task<List<AttendanceRow>> GetSheetAsync(int lessonId)
  {
    var user = Session.RequireUser();
    using var db = CreateContext();
    var lesson = await db.Lessons.Include(l => l.ClassRoom).FirstOrDefaultAsync(l => l.Id == lessonId)
      ?? throw new BusinessException("Không Tìm Thấy Buổi Học");
    if (user.IsTeacher && lesson.TeacherId != user.Id && lesson.ClassRoom!.TeacherId != user.Id)
      throw new BusinessException("Bạn Chỉ Xem Được Buổi Học Của Lớp Mình Dạy");
    var enrollments = await db.Enrollments.AsNoTracking()
      .Include(e => e.Student)
      .Include(e => e.Attendances.Select(a => a.Lesson))
      .Where(e => e.ClassRoomId == lesson.ClassRoomId && e.Status != EnrollmentStatus.Dropped)
      .ToListAsync();
    return enrollments.OrderBy(e => e.Student!.FullName).Select(e =>
    {
      var current = e.Attendances.FirstOrDefault(a => a.LessonId == lessonId);
      var before = e.Attendances.Where(a => a.Lesson!.LessonNo < lesson.LessonNo && a.Lesson.Status == LessonStatus.Done).ToList();
      double rate = before.Count == 0 ? 100 : Enrollment.AttendanceRate(before.Count(a => a.IsAttended), before.Count);
      return new AttendanceRow
      {
        EnrollmentId = e.Id,
        StudentCode = e.Student!.Code,
        StudentName = e.Student.FullName,
        Phone = e.Student.Phone,
        Status = current?.Status ?? (e.Status == EnrollmentStatus.Reserved ? AttendanceStatus.AbsentExcused : AttendanceStatus.Present),
        Note = current?.Note,
        AttendanceRate = rate
      };
    }).ToList();
  }
  public async Task SaveAsync(int lessonId, IEnumerable<AttendanceRow> rows, string? content)
  {
    var user = Session.Require(Permission.TakeAttendance);
    using var db = CreateContext();
    var lesson = await db.Lessons.Include(s => s.ClassRoom)
      .FirstOrDefaultAsync(s => s.Id == lessonId)
      ?? throw new BusinessException("Không Tìm Thấy Buổi Học");
    var cls = lesson.ClassRoom!;
    if (lesson.Status == LessonStatus.Cancelled)
      throw new BusinessException("Buổi Học Đã Bị Hủy");
    if (lesson.Date.Date > DateTime.Today)
      throw new BusinessException("Chưa Đến Ngày Học, Không Thể Điểm Danh Trước");
    if (cls.Status != ClassStatus.InProgress && cls.Status != ClassStatus.Finished)
      throw new BusinessException("Lớp Chưa Khai Giảng · Hãy Khai Giảng Lớp Trước Khi Điểm Danh");
    if (cls.Status == ClassStatus.Finished && !user.IsManager)
      throw new BusinessException("Lớp Đã Kết Thúc, Chỉ Quản Lý Được Sửa Điểm Danh");
    if (user.IsTeacher)
    {
      if (lesson.TeacherId != user.Id)
        throw new BusinessException("Bạn Không Phải Giáo Viên Dạy Buổi Này");
      if (lesson.Date.Date != DateTime.Today)
        throw new BusinessException("Giáo Viên Chỉ Được Điểm Danh Trong Ngày Học · Liên Hệ Quản Lý Để Sửa");
    }
    var rowList = rows.ToList();
    if (rowList.Count == 0)
      throw new BusinessException("Lớp Chưa Có Học Viên Để Điểm Danh");
    var validIds = await db.Enrollments
      .Where(e => e.ClassRoomId == cls.Id && e.Status != EnrollmentStatus.Dropped)
      .Select(e => e.Id).ToListAsync();
    var existing = await db.Attendances.Where(a => a.LessonId == lessonId).ToListAsync();
    foreach (var row in rowList)
    {
      if (!validIds.Contains(row.EnrollmentId))
        throw new BusinessException("Danh Sách Có Học Viên Không Thuộc Lớp Này");
      var att = existing.FirstOrDefault(a => a.EnrollmentId == row.EnrollmentId);
      if (att == null)
      {
        att = new Attendance { LessonId = lessonId, EnrollmentId = row.EnrollmentId };
        db.Attendances.Add(att);
        existing.Add(att);
      }
      att.Status = row.Status;
      att.Note = row.Note;
    }
    lesson.Status = LessonStatus.Done;
    lesson.Content = string.IsNullOrWhiteSpace(content) ? null : content!.Trim();
    await db.SaveChangesAsync();
  }
}