using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services.Common;
using EduFlow.Services.Scheduling;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
namespace EduFlow.Services;
public interface IClassService
{
  Task<List<ClassRoom>> GetAllAsync(ClassStatus? status = null);
  Task<List<ClassRoom>> GetMyClassesAsync();
  Task<List<Lesson>> GetLessonsAsync(int classId);
  Task<List<Lesson>> GetWeekScheduleAsync(DateTime weekStart);
  Task<ClassRoom> CreateAsync(ClassRoom newClass);
  Task<ClassRoom> UpdateAsync(ClassRoom updatedClass);
  Task StartAsync(int classId);
  Task FinishAsync(int classId);
  Task CancelAsync(int classId, string reason);
  Task DeleteAsync(int classId);
  Task CancelLessonAsync(int lessonId, string reason);
}
public class ClassService : BaseService, IClassService
{
  public ClassService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
  public async Task<List<ClassRoom>> GetAllAsync(ClassStatus? status = null)
  {
    var user = Session.RequireUser();
    using var db = CreateContext();
    var query = IncludeAll(db.Classes.AsNoTracking());
    if (user.IsTeacher) query = query.Where(c => c.TeacherId == user.Id);
    if (status.HasValue) query = query.Where(c => c.Status == status);
    return await query.OrderByDescending(c => c.StartDate).ToListAsync();
  }
  public async Task<List<ClassRoom>> GetMyClassesAsync()
  {
    var user = Session.RequireUser();
    using var db = CreateContext();
    var query = IncludeAll(db.Classes.AsNoTracking()).Where(c => c.Status != ClassStatus.Cancelled);
    if (user.IsTeacher)
      query = query.Where(c => c.TeacherId == user.Id || c.Lessons.Any(l => l.TeacherId == user.Id));
    return await query.OrderBy(c => c.Code).ToListAsync();
  }
  public async Task<List<Lesson>> GetLessonsAsync(int classId)
  {
    var user = Session.RequireUser();
    using var db = CreateContext();
    var query = db.Lessons.AsNoTracking()
      .Include(l => l.PhysicalRoom).Include(l => l.Teacher)
      .Where(s => s.ClassRoomId == classId);
    if (user.IsTeacher)
      query = query.Where(s => s.TeacherId == user.Id || s.ClassRoom!.TeacherId == user.Id);
    return await query.OrderBy(s => s.LessonNo).ToListAsync();
  }
  public async Task<List<Lesson>> GetWeekScheduleAsync(DateTime weekStart)
  {
    var user = Session.RequireUser();
    DateTime from = weekStart.Date, to = from.AddDays(7);
    using var db = CreateContext();
    var query = db.Lessons.AsNoTracking()
      .Include(s => s.ClassRoom!.Course)
      .Include(s => s.PhysicalRoom)
      .Include(s => s.Teacher)
      .Where(s => s.Date >= from && s.Date < to
        && s.Status != LessonStatus.Cancelled
        && s.ClassRoom!.Status != ClassStatus.Cancelled);
    if (user.IsTeacher)
      query = query.Where(s => s.TeacherId == user.Id || (s.ClassRoom != null && s.ClassRoom.TeacherId == user.Id));
    var list = await query.ToListAsync();
    return list.OrderBy(s => s.Date).ThenBy(s => s.StartTime).ToList();
  }
  public async Task<ClassRoom> CreateAsync(ClassRoom input)
  {
    Session.Require(Permission.ManageClasses);
    using var db = CreateContext();
    var course = await db.Courses.FindAsync(input.CourseId) ?? throw new BusinessException("Phải Chọn Khóa Học");
    var teacher = await db.Users.FindAsync(input.TeacherId) ?? throw new BusinessException("Phải Chọn Giáo Viên");
    var room = await db.Rooms.FindAsync(input.PhysicalRoomId) ?? throw new BusinessException("Phải Chọn Phòng Học");
    if (!course.IsActive) throw new BusinessException("Khóa Học Đã Ngừng Mở Lớp");
    if (teacher.Role != UserRole.Teacher) throw new BusinessException("Người Được Phân Công Không Phải Giáo Viên");
    if (!teacher.IsActive) throw new BusinessException("Tài Khoản Giáo Viên Đã Bị Khóa, Không Thể Phân Công");
    if (!room.IsActive) throw new BusinessException("Phòng Học Đang Ngừng Sử Dụng");
    if (input.MinStudents <= 0 || input.MaxStudents <= 0)
      throw new BusinessException("Sĩ Số Tối Thiểu & Tối Đa Phải Lớn Hơn 0");
    if (input.MinStudents > input.MaxStudents)
      throw new BusinessException("Sĩ Số Tối Thiểu Không Được Lớn Hơn Sĩ Số Tối Đa");
    if (input.MaxStudents > room.Capacity)
      throw new BusinessException($"Sĩ Số Tối Đa Vượt Sức Chứa Phòng {room.Name} · Tối Đa {room.Capacity} Chỗ");
    if (input.StartDate.Date < DateTime.Today)
      throw new BusinessException("Ngày Khai Giảng Không Được Ở Quá Khứ");
    var newClass = new ClassRoom
    {
      CourseId = course.Id,
      TeacherId = teacher.Id,
      PhysicalRoomId = room.Id,
      StartDate = input.StartDate.Date,
      Days = input.Days,
      StartTime = input.StartTime,
      EndTime = input.EndTime,
      MinStudents = input.MinStudents,
      MaxStudents = input.MaxStudents,
      Status = ClassStatus.Open
    };
    var lessons = ScheduleCalculator.GenerateLessons(newClass, course.SessionCount);
    DateTime from = lessons.First().Date, to = lessons.Last().Date;
    var existing = await db.Lessons.AsNoTracking()
      .Include(s => s.ClassRoom)
      .Where(s => s.Date >= from && s.Date <= to
        && s.Status != LessonStatus.Cancelled
        && s.ClassRoom!.Status != ClassStatus.Cancelled
        && (s.PhysicalRoomId == room.Id || s.TeacherId == teacher.Id))
      .ToListAsync();
    if (ScheduleCalculator.FindConflict(lessons, existing) is { } c)
    {
      string who = c.Existing.PhysicalRoomId == room.Id ? $"Phòng {room.Name}" : $"Giáo Viên {teacher.FullName}";
      throw new BusinessException($"Trùng Lịch {who} Ngày {c.New.Date:dd/MM/yyyy} Với Lớp {c.Existing.ClassRoom?.Code}");
    }
    if (!string.IsNullOrWhiteSpace(input.Code))
    {
      newClass.Code = input.Code.Trim();
    }
    else
    {
      var existingCodes = await db.Classes.Where(x => x.CourseId == course.Id).Select(x => x.Code).ToListAsync();
      if (!existingCodes.Contains(course.Code))
      {
        newClass.Code = course.Code;
      }
      else
      {
        int nextNo = 2;
        while (existingCodes.Contains($"{course.Code} · {nextNo}")) nextNo++;
        newClass.Code = $"{course.Code} · {nextNo}";
      }
    }
    foreach (var s in lessons) newClass.Lessons.Add(s);
    db.Classes.Add(newClass);
    await db.SaveChangesAsync();
    return newClass;
  }
  public async Task<ClassRoom> UpdateAsync(ClassRoom input)
  {
    Session.Require(Permission.ManageClasses);
    using var db = CreateContext();
    var cls = await LoadForStatusAsync(db, input.Id);
    if (cls.Status is ClassStatus.Finished or ClassStatus.Cancelled)
      throw new BusinessException("Lớp Đã Kết Thúc Hoặc Đã Hủy, Không Thể Cập Nhật");
    var course = await db.Courses.FindAsync(input.CourseId) ?? throw new BusinessException("Phải Chọn Khóa Học");
    var teacher = await db.Users.FindAsync(input.TeacherId) ?? throw new BusinessException("Phải Chọn Giáo Viên");
    var room = await db.Rooms.FindAsync(input.PhysicalRoomId) ?? throw new BusinessException("Phải Chọn Phòng Học");
    if (teacher.Role != UserRole.Teacher) throw new BusinessException("Người Được Phân Công Không Phải Giáo Viên");
    if (!teacher.IsActive) throw new BusinessException("Tài Khoản Giáo Viên Đã Bị Khóa, Không Thể Phân Công");
    if (!room.IsActive) throw new BusinessException("Phòng Học Đang Ngừng Sử Dụng");
    if (input.MinStudents <= 0 || input.MaxStudents <= 0)
      throw new BusinessException("Sĩ Số Tối Thiểu & Tối Đa Phải Lớn Hơn 0");
    if (input.MinStudents > input.MaxStudents)
      throw new BusinessException("Sĩ Số Tối Thiểu Không Được Lớn Hơn Sĩ Số Tối Đa");
    if (input.MaxStudents > room.Capacity)
      throw new BusinessException($"Sĩ Số Tối Đa Vượt Sức Chứa Phòng {room.Name} · Tối Đa {room.Capacity} Chỗ");
    if (input.MaxStudents < cls.ActiveStudentCount)
      throw new BusinessException($"Lớp Đang Có {cls.ActiveStudentCount} Học Viên, Không Thể Giảm Sĩ Số Tối Đa Xuống {input.MaxStudents}");
    bool scheduleChanged = cls.CourseId != course.Id || cls.StartDate.Date != input.StartDate.Date
      || cls.Days != input.Days || cls.StartTime != input.StartTime || cls.EndTime != input.EndTime;
    if (scheduleChanged)
    {
      if (cls.Status != ClassStatus.Open || cls.Lessons.Any(l => l.Status == LessonStatus.Done))
        throw new BusinessException("Lớp Đã Học Hoặc Đã Điểm Danh, Chỉ Được Đổi Giáo Viên, Phòng Học Hoặc Sĩ Số");
      if (cls.CourseId != course.Id && cls.Enrollments.Any(e => e.Status != EnrollmentStatus.Dropped))
        throw new BusinessException("Lớp Đã Có Học Viên Ghi Danh, Không Thể Đổi Khóa Học");
      cls.CourseId = course.Id;
      cls.TeacherId = teacher.Id;
      cls.PhysicalRoomId = room.Id;
      cls.StartDate = input.StartDate.Date;
      cls.Days = input.Days;
      cls.StartTime = input.StartTime;
      cls.EndTime = input.EndTime;
      var lessons = ScheduleCalculator.GenerateLessons(cls, course.SessionCount);
      DateTime from = lessons.First().Date, to = lessons.Last().Date;
      var existing = await db.Lessons.AsNoTracking()
        .Include(s => s.ClassRoom)
        .Where(s => s.ClassRoomId != cls.Id && s.Date >= from && s.Date <= to
          && s.Status != LessonStatus.Cancelled
          && s.ClassRoom!.Status != ClassStatus.Cancelled
          && (s.PhysicalRoomId == room.Id || s.TeacherId == teacher.Id))
        .ToListAsync();
      if (ScheduleCalculator.FindConflict(lessons, existing) is { } c)
      {
        string who = c.Existing.PhysicalRoomId == room.Id ? $"Phòng {room.Name}" : $"Giáo Viên {teacher.FullName}";
        throw new BusinessException($"Trùng Lịch {who} Ngày {c.New.Date:dd/MM/yyyy} Với Lớp {c.Existing.ClassRoom?.Code}");
      }
      using var tx = db.Database.BeginTransaction();
      db.Lessons.RemoveRange(cls.Lessons.ToList());
      await db.SaveChangesAsync();
      foreach (var s in lessons) cls.Lessons.Add(s);
      cls.MinStudents = input.MinStudents;
      cls.MaxStudents = input.MaxStudents;
      await SaveWithConcurrencyAsync(db, "Lớp Vừa Được Người Khác Cập Nhật · Hãy Tải Lại Rồi Thử Lại");
      tx.Commit();
      return cls;
    }
    else if (cls.TeacherId != teacher.Id || cls.PhysicalRoomId != room.Id)
    {
      var scheduled = cls.Lessons.Where(l => l.Status == LessonStatus.Scheduled).ToList();
      foreach (var l in scheduled)
      {
        l.TeacherId = teacher.Id;
        l.PhysicalRoomId = room.Id;
      }
      if (scheduled.Count > 0)
      {
        DateTime from = scheduled.Min(l => l.Date), to = scheduled.Max(l => l.Date);
        var existing = await db.Lessons.AsNoTracking()
          .Include(s => s.ClassRoom)
          .Where(s => s.ClassRoomId != cls.Id && s.Date >= from && s.Date <= to
            && s.Status != LessonStatus.Cancelled
            && s.ClassRoom!.Status != ClassStatus.Cancelled
            && (s.PhysicalRoomId == room.Id || s.TeacherId == teacher.Id))
          .ToListAsync();
        if (ScheduleCalculator.FindConflict(scheduled, existing) is { } c)
        {
          string who = c.Existing.PhysicalRoomId == room.Id ? $"Phòng {room.Name}" : $"Giáo Viên {teacher.FullName}";
          throw new BusinessException($"Trùng Lịch {who} Ngày {c.New.Date:dd/MM/yyyy} Với Lớp {c.Existing.ClassRoom?.Code}");
        }
      }
      cls.TeacherId = teacher.Id;
      cls.PhysicalRoomId = room.Id;
    }
    cls.MinStudents = input.MinStudents;
    cls.MaxStudents = input.MaxStudents;
    await SaveWithConcurrencyAsync(db, "Lớp Vừa Được Người Khác Cập Nhật · Hãy Tải Lại Rồi Thử Lại");
    return cls;
  }
  public async Task StartAsync(int classId)
  {
    Session.Require(Permission.ManageClasses);
    using var db = CreateContext();
    var cls = await LoadForStatusAsync(db, classId);
    EnsureCanMove(cls, ClassStatus.InProgress);
    if (!cls.Lessons.Any(l => l.Status != LessonStatus.Cancelled))
      throw new BusinessException("Lớp Chưa Có Lịch Học");
    if (cls.ActiveStudentCount < cls.MinStudents)
      throw new BusinessException($"Lớp Mới Có {cls.ActiveStudentCount} Học Viên, Cần Tối Thiểu {cls.MinStudents} Để Khai Giảng");
    cls.Status = ClassStatus.InProgress;
    await SaveWithConcurrencyAsync(db, "Lớp Vừa Được Người Khác Cập Nhật · Hãy Tải Lại Rồi Thử Lại");
  }
  public async Task FinishAsync(int classId)
  {
    Session.Require(Permission.ManageClasses);
    using var db = CreateContext();
    var cls = await LoadForStatusAsync(db, classId);
    EnsureCanMove(cls, ClassStatus.Finished);
    var pending = cls.Lessons.Where(l => l.Status == LessonStatus.Scheduled).OrderBy(l => l.LessonNo).ToList();
    if (pending.Count > 0)
      throw new BusinessException($"Còn {pending.Count} Buổi Chưa Điểm Danh Hoặc Chưa Hủy · Từ Buổi {pending[0].LessonNo}");
    var learners = cls.Enrollments.Where(e => e.Status == EnrollmentStatus.Active).ToList();
    var missing = learners.Where(e => e.TotalScore == null).ToList();
    if (missing.Count > 0)
      throw new BusinessException($"Còn {missing.Count} Học Viên Chưa Có Đủ Điểm Quá Trình & Cuối Kỳ");
    int lessonsDone = cls.Lessons.Count(l => l.Status == LessonStatus.Done);
    var attendedByEnrollment = await db.Attendances
      .Where(a => a.Lesson!.ClassRoomId == cls.Id && a.Lesson.Status == LessonStatus.Done
        && (a.Status == AttendanceStatus.Present || a.Status == AttendanceStatus.Late))
      .GroupBy(a => a.EnrollmentId)
      .Select(g => new { g.Key, Count = g.Count() })
      .ToDictionaryAsync(x => x.Key, x => x.Count);
    foreach (var e in learners)
    {
      double rate = Enrollment.AttendanceRate(attendedByEnrollment.TryGetValue(e.Id, out int attended) ? attended : 0, lessonsDone);
      e.Status = Enrollment.IsPassed(e.TotalScore!.Value, rate, Enrollment.PassScore, Enrollment.MinAttendancePercent)
        ? EnrollmentStatus.Completed
        : EnrollmentStatus.Failed;
    }
    cls.Status = ClassStatus.Finished;
    await SaveWithConcurrencyAsync(db, "Lớp Vừa Được Người Khác Cập Nhật · Hãy Tải Lại Rồi Thử Lại");
  }
  public async Task CancelAsync(int classId, string reason)
  {
    Session.Require(Permission.CancelClass);
    if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length < 5)
      throw new BusinessException("Phải Nhập Lý Do Hủy Lớp · Ít Nhất 5 Ký Tự");
    using var db = CreateContext();
    var cls = await LoadForStatusAsync(db, classId);
    EnsureCanMove(cls, ClassStatus.Cancelled);
    if (cls.ActiveStudentCount > 0)
      throw new BusinessException("Lớp Còn Học Viên Đang Ghi Danh · Hãy Chuyển Lớp Hoặc Cho Nghỉ Trước, Học Phí Xử Lý Hoàn Tiền Riêng");
    cls.Status = ClassStatus.Cancelled;
    cls.CancelReason = reason.Trim();
    foreach (var l in cls.Lessons.Where(l => l.Status == LessonStatus.Scheduled))
      l.Status = LessonStatus.Cancelled;
    await SaveWithConcurrencyAsync(db, "Lớp Vừa Được Người Khác Cập Nhật · Hãy Tải Lại Rồi Thử Lại");
  }
  public async Task DeleteAsync(int classId)
  {
    Session.Require(Permission.ManageClasses);
    using var db = CreateContext();
    var cls = await db.Classes
      .Include(c => c.Enrollments)
      .Include(c => c.Lessons.Select(l => l.Attendances))
      .FirstOrDefaultAsync(c => c.Id == classId)
      ?? throw new BusinessException("Không Tìm Thấy Lớp");
    if (cls.Status is ClassStatus.InProgress or ClassStatus.Finished)
      throw new BusinessException($"Lớp {cls.Code} Đang Ở Trạng Thái {cls.Status.ToText()}, Không Thể Xóa · Hãy Dùng Chức Năng Hủy Lớp");
    if (cls.Enrollments.Any() || cls.ActiveStudentCount > 0)
      throw new BusinessException($"Lớp {cls.Code} Đã Có Học Viên Tham Gia, Không Thể Xóa · Hãy Dùng Chức Năng Hủy Lớp");
    bool hasTuition = await db.TuitionFees.AnyAsync(f => f.ClassRoomId == classId);
    if (hasTuition)
      throw new BusinessException($"Lớp {cls.Code} Đã Có Học Phí Liên Kết, Không Thể Xóa · Hãy Dùng Chức Năng Hủy Lớp");
    bool hasAttendance = await db.Attendances.AnyAsync(a => a.Lesson!.ClassRoomId == classId);
    if (hasAttendance)
      throw new BusinessException($"Lớp {cls.Code} Đã Phát Sinh Điểm Danh Buổi Học, Không Thể Xóa · Hãy Dùng Chức Năng Hủy Lớp");
    foreach (var l in cls.Lessons.ToList()) db.Attendances.RemoveRange(l.Attendances);
    db.Lessons.RemoveRange(cls.Lessons);
    db.Classes.Remove(cls);
    try
    {
      await db.SaveChangesAsync();
    }
    catch (DbUpdateException)
    {
      throw new BusinessException($"Lớp {cls.Code} Đang Có Dữ Liệu Học Viên Hoặc Học Phí Liên Kết, Không Thể Xóa · Hãy Dùng Chức Năng Hủy Lớp");
    }
  }
  public async Task CancelLessonAsync(int lessonId, string reason)
  {
    Session.Require(Permission.ManageClasses);
    if (string.IsNullOrWhiteSpace(reason))
      throw new BusinessException("Phải Nhập Lý Do Hủy Buổi Học");
    using var db = CreateContext();
    var lesson = await db.Lessons.Include(l => l.ClassRoom).FirstOrDefaultAsync(l => l.Id == lessonId)
      ?? throw new BusinessException("Không Tìm Thấy Buổi Học");
    if (lesson.ClassRoom!.Status is ClassStatus.Finished or ClassStatus.Cancelled)
      throw new BusinessException("Lớp Đã Kết Thúc Hoặc Đã Hủy, Không Thể Hủy Buổi Học");
    if (lesson.Status != LessonStatus.Scheduled)
      throw new BusinessException("Chỉ Hủy Được Buổi Chưa Dạy");
    lesson.Status = LessonStatus.Cancelled;
    lesson.Content = $"Nghỉ: {reason.Trim()}";
    await db.SaveChangesAsync();
  }
  private static async Task<ClassRoom> LoadForStatusAsync(EduFlowDbContext db, int classId) =>
    await db.Classes.Include(c => c.Enrollments).Include(c => c.Lessons)
      .FirstOrDefaultAsync(c => c.Id == classId)
    ?? throw new BusinessException("Không Tìm Thấy Lớp");
  private static void EnsureCanMove(ClassRoom cls, ClassStatus to)
  {
    if (!ClassRoom.CanMove(cls.Status, to))
      throw new BusinessException($"Không Thể Chuyển Lớp Từ \"{cls.Status.ToText()}\" Sang \"{to.ToText()}\"");
  }
  private static IQueryable<ClassRoom> IncludeAll(IQueryable<ClassRoom> q) =>
    q.Include(c => c.Course)
     .Include(c => c.Teacher)
     .Include(c => c.PhysicalRoom)
     .Include(c => c.Enrollments);
}