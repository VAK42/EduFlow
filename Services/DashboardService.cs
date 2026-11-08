using EduFlow.Data;
using EduFlow.Models.Enums;
using EduFlow.Services.Common;
using EduFlow.Services.Dtos;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
namespace EduFlow.Services;
public interface IDashboardService
{
  Task<DashboardSummary> GetSummaryAsync();
}
public class DashboardService : BaseService, IDashboardService
{
  public DashboardService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
  public async Task<DashboardSummary> GetSummaryAsync()
  {
    var user = Session.RequireUser();
    bool finance = user.Role == UserRole.Admin;
    using var db = CreateContext();
    int activeStudents = await db.Students.CountAsync(s => s.IsActive);
    int openClasses = await db.Classes.CountAsync(c => c.Status == ClassStatus.Open);
    int runningClasses = await db.Classes.CountAsync(c => c.Status == ClassStatus.InProgress);
    var monthStart = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
    var sixMonthsAgo = monthStart.AddMonths(-5);
    var collections = new List<MonthCollection>();
    decimal totalDebt = 0;
    int debtorCount = 0;
    var topDebts = new List<DebtRow>();
    if (finance)
    {
      var paidFees = await db.TuitionFees.AsNoTracking()
        .Where(f => f.IsPaid)
        .ToListAsync();
      collections = Enumerable.Range(0, 6)
        .Select(i => sixMonthsAgo.AddMonths(i))
        .Select(m => new MonthCollection(m.Year, m.Month,
          paidFees.Where(f => (f.PaidAt.HasValue && f.PaidAt.Value.Year == m.Year && f.PaidAt.Value.Month == m.Month)
            || (!f.PaidAt.HasValue && f.Year == m.Year && f.Month == m.Month)).Sum(f => f.Amount)))
        .ToList();
      var unpaidFees = await db.TuitionFees.AsNoTracking()
        .Include(f => f.Student)
        .Include(f => f.ClassRoom)
        .Where(f => !f.IsPaid)
        .ToListAsync();
      totalDebt = unpaidFees.Sum(f => f.Amount);
      debtorCount = unpaidFees.Select(f => f.StudentId).Distinct().Count();
      topDebts = unpaidFees
        .GroupBy(f => f.StudentId)
        .Select(g =>
        {
          var first = g.First();
          var student = first.Student!;
          var classCode = first.ClassRoom?.Code ?? "Chưa Xếp Lớp";
          var debt = g.Sum(x => x.Amount);
          return new DebtRow(first.Id, student.Code, student.FullName, student.Phone, classCode, debt, 0, debt);
        })
        .OrderByDescending(d => d.Debt)
        .Take(8)
        .ToList();
    }
    var classQuery = db.Classes.AsNoTracking()
      .Include(c => c.Course).Include(c => c.Enrollments)
      .Where(c => c.Status == ClassStatus.Open || c.Status == ClassStatus.InProgress);
    if (user.IsTeacher) classQuery = classQuery.Where(c => c.TeacherId == user.Id);
    var classes = await classQuery.OrderBy(c => c.Code).ToListAsync();
    var classFill = classes.Select(c => new ClassFillRow(c.Code, c.Course!.Name, c.ActiveStudentCount, c.MaxStudents)).ToList();
    var today = DateTime.Today;
    var lessonQuery = db.Lessons.AsNoTracking()
      .Include(s => s.ClassRoom!.Course)
      .Include(s => s.PhysicalRoom)
      .Include(s => s.Teacher)
      .Where(s => s.Date == today && s.Status != LessonStatus.Cancelled
        && s.ClassRoom!.Status != ClassStatus.Cancelled);
    if (user.IsTeacher) lessonQuery = lessonQuery.Where(s => s.TeacherId == user.Id);
    var lessons = (await lessonQuery.ToListAsync()).OrderBy(s => s.StartTime).ToList();
    var todayRows = lessons.Select(s => new TodayLessonRow(
      s.ClassRoom!.Code, s.ClassRoom.Course!.Name,
      $"{s.StartTime:hh\\:mm} - {s.EndTime:hh\\:mm}",
      s.PhysicalRoom!.Name, s.Teacher!.FullName)).ToList();
    return new DashboardSummary(
      activeStudents, openClasses, runningClasses,
      finance && collections.Count > 0 ? collections[collections.Count - 1].Amount : 0,
      finance && collections.Count > 1 ? collections[collections.Count - 2].Amount : 0,
      totalDebt, debtorCount, finance, todayRows, collections, classFill, topDebts);
  }
}