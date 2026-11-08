using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services.Common;
using EduFlow.Services.Dtos;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
namespace EduFlow.Services;
public interface IScoreService
{
  Task<List<ScoreRow>> GetSheetAsync(int classId);
  Task SaveAsync(int classId, IEnumerable<ScoreRow> rows);
}
public class ScoreService : BaseService, IScoreService
{
  public ScoreService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
  public async Task<List<ScoreRow>> GetSheetAsync(int classId)
  {
    var user = Session.RequireUser();
    using var db = CreateContext();
    var cls = await db.Classes.Include(c => c.Lessons).FirstOrDefaultAsync(c => c.Id == classId)
      ?? throw new BusinessException("Không Tìm Thấy Lớp");
    if (user.IsTeacher && cls.TeacherId != user.Id && !cls.Lessons.Any(l => l.TeacherId == user.Id))
      throw new BusinessException("Bạn Chỉ Xem Được Điểm Của Lớp Mình Dạy");
    int lessonsDone = cls.Lessons.Count(l => l.Status == LessonStatus.Done);
    var enrollments = await db.Enrollments.AsNoTracking()
      .Include(e => e.Student)
      .Include(e => e.Attendances.Select(a => a.Lesson))
      .Where(e => e.ClassRoomId == classId && e.Status != EnrollmentStatus.Dropped)
      .ToListAsync();
    return enrollments.OrderBy(e => e.Student!.FullName).Select(e =>
    {
      int attended = e.Attendances.Count(a => a.Lesson!.Status == LessonStatus.Done && a.IsAttended);
      double rate = Enrollment.AttendanceRate(attended, lessonsDone);
      string result = e.Status switch
      {
        EnrollmentStatus.Completed => "Đạt",
        EnrollmentStatus.Failed => "Không Đạt",
        _ when e.TotalScore is null => "Chưa Đủ Điểm",
        _ => Enrollment.IsPassed(e.TotalScore.Value, rate, Enrollment.PassScore, Enrollment.MinAttendancePercent)
          ? "Dự Kiến Đạt" : "Dự Kiến Không Đạt"
      };
      return new ScoreRow
      {
        EnrollmentId = e.Id,
        StudentCode = e.Student!.Code,
        StudentName = e.Student.FullName,
        Phone = e.Student.Phone,
        ProcessScore = e.ProcessScore,
        FinalScore = e.FinalScore,
        TotalScore = e.TotalScore,
        AttendanceRate = rate,
        ResultText = result,
        PassScore = Enrollment.PassScore,
        MinAttendancePercent = Enrollment.MinAttendancePercent
      };
    }).ToList();
  }
  public async Task SaveAsync(int classId, IEnumerable<ScoreRow> rows)
  {
    var user = Session.Require(Permission.EnterScores);
    using var db = CreateContext();
    var cls = await db.Classes.Include(c => c.Lessons).FirstOrDefaultAsync(c => c.Id == classId)
      ?? throw new BusinessException("Không Tìm Thấy Lớp");
    if (user.IsTeacher && cls.TeacherId != user.Id && !cls.Lessons.Any(l => l.TeacherId == user.Id))
      throw new BusinessException("Bạn Chỉ Nhập Được Điểm Cho Lớp Mình Dạy");
    if (cls.Status == ClassStatus.Open || cls.Status == ClassStatus.Cancelled)
      throw new BusinessException("Chỉ Nhập Điểm Cho Lớp Đang Học");
    if (cls.Status == ClassStatus.Finished && !user.IsManager)
      throw new BusinessException("Lớp Đã Kết Thúc, Chỉ Quản Lý Được Sửa Điểm");
    var rowList = rows.ToList();
    if (rowList.Count == 0)
      throw new BusinessException("Lớp Chưa Có Học Viên Để Nhập Điểm");
    var enrollments = await db.Enrollments
      .Include(e => e.Attendances.Select(a => a.Lesson))
      .Where(e => e.ClassRoomId == classId && e.Status != EnrollmentStatus.Dropped)
      .ToDictionaryAsync(e => e.Id);
    int lessonsDone = cls.Lessons.Count(l => l.Status == LessonStatus.Done);
    foreach (var row in rowList)
    {
      if (!enrollments.TryGetValue(row.EnrollmentId, out var e))
        throw new BusinessException("Danh Sách Có Học Viên Không Thuộc Lớp Này");
      if (cls.Status == ClassStatus.Finished && (row.ProcessScore == null || row.FinalScore == null))
        throw new BusinessException($"Lớp Đã Kết Thúc, Không Thể Để Trống Điểm Của {row.StudentName}");
      try
      {
        e.SetScores(row.ProcessScore, row.FinalScore);
      }
      catch (BusinessException ex)
      {
        throw new BusinessException($"{row.StudentName}: {ex.Message}");
      }
      if (cls.Status == ClassStatus.Finished && e.TotalScore is { } total
        && e.Status is EnrollmentStatus.Completed or EnrollmentStatus.Failed)
      {
        int attended = e.Attendances.Count(a => a.Lesson!.Status == LessonStatus.Done && a.IsAttended);
        double rate = Enrollment.AttendanceRate(attended, lessonsDone);
        e.Status = Enrollment.IsPassed(total, rate, Enrollment.PassScore, Enrollment.MinAttendancePercent)
          ? EnrollmentStatus.Completed : EnrollmentStatus.Failed;
      }
    }
    await SaveWithConcurrencyAsync(db, "Điểm Vừa Được Người Khác Cập Nhật · Hãy Tải Lại Rồi Nhập Lại");
  }
}