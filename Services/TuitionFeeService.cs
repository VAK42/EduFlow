using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services.Common;
namespace EduFlow.Services
{
  public interface ITuitionFeeService
  {
    Task<List<TuitionFee>> GetTuitionFeesAsync(int? month, int? year, FeeType? feeType, bool? isPaid, string? keyword);
    Task<List<Student>> GetEligibleStudentsAsync();
    Task<List<ClassRoom>> GetEligibleClassesAsync();
    Task<TuitionFee> SaveTuitionFeeAsync(TuitionFee input);
    Task TogglePaidAsync(int feeId);
    Task DeleteTuitionFeeAsync(int feeId);
  }
  public class TuitionFeeService : BaseService, ITuitionFeeService
  {
    public TuitionFeeService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
    public async Task<List<TuitionFee>> GetTuitionFeesAsync(int? month, int? year, FeeType? feeType, bool? isPaid, string? keyword)
    {
      var current = Session.RequireUser();
      if (current.Role != UserRole.Admin)
        throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Học Phí");
      using var db = CreateContext();
      var q = db.TuitionFees.AsNoTracking()
        .Include(f => f.Student)
        .Include(f => f.ClassRoom)
        .Include(f => f.ClassRoom!.Course)
        .AsQueryable();
      if (feeType.HasValue)
        q = q.Where(f => f.FeeType == feeType.Value);
      if (isPaid.HasValue)
        q = q.Where(f => f.IsPaid == isPaid.Value);
      if (month.HasValue && month.Value > 0)
        q = q.Where(f => f.FeeType == FeeType.OneTime || f.Month == month.Value);
      if (year.HasValue && year.Value > 0)
        q = q.Where(f => f.FeeType == FeeType.OneTime || f.Year == year.Value);
      if (!string.IsNullOrWhiteSpace(keyword))
      {
        string kw = keyword!.Trim().ToLower();
        q = q.Where(f => f.Student!.FullName.ToLower().Contains(kw) || f.Student.Code.ToLower().Contains(kw) || f.Student.Phone.Contains(kw));
      }
      return await q.OrderBy(f => f.IsPaid)
        .ThenByDescending(f => f.Year)
        .ThenByDescending(f => f.Month)
        .ThenBy(f => f.Student!.FullName)
        .ToListAsync();
    }
    public async Task<List<Student>> GetEligibleStudentsAsync()
    {
      var current = Session.RequireUser();
      if (current.Role != UserRole.Admin)
        throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Học Phí");
      using var db = CreateContext();
      return await db.Students.AsNoTracking()
        .Where(s => s.IsActive)
        .OrderBy(s => s.FullName)
        .ToListAsync();
    }
    public async Task<List<ClassRoom>> GetEligibleClassesAsync()
    {
      var current = Session.RequireUser();
      if (current.Role != UserRole.Admin)
        throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Học Phí");
      using var db = CreateContext();
      return await db.Classes.AsNoTracking()
        .Include(c => c.Course)
        .Where(c => c.Status != ClassStatus.Cancelled)
        .OrderByDescending(c => c.CreatedAt)
        .ToListAsync();
    }
    public async Task<TuitionFee> SaveTuitionFeeAsync(TuitionFee input)
    {
      var current = Session.RequireUser();
      if (current.Role != UserRole.Admin)
        throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Học Phí");
      if (input.StudentId <= 0)
        throw new BusinessException("Vui Lòng Chọn Học Viên");
      if (input.Amount <= 0)
        throw new BusinessException("Số Tiền Học Phí Phải Lớn Hơn 0");
      if (input.FeeType == FeeType.Monthly)
      {
        if (input.Month < 1 || input.Month > 12)
          throw new BusinessException("Tháng Không Hợp Lệ");
        if (input.Year < 2000 || input.Year > 2100)
          throw new BusinessException("Năm Không Hợp Lệ");
      }
      using var db = CreateContext();
      int? classId = input.ClassRoomId > 0 ? input.ClassRoomId : null;
      if (input.FeeType == FeeType.OneTime)
      {
        bool hasOneTime = classId.HasValue
          ? await db.TuitionFees.AnyAsync(f => f.StudentId == input.StudentId && (f.ClassRoomId == classId.Value || f.ClassRoomId == null) && f.FeeType == FeeType.OneTime && f.Id != input.Id)
          : await db.TuitionFees.AnyAsync(f => f.StudentId == input.StudentId && f.FeeType == FeeType.OneTime && f.Id != input.Id);
        if (hasOneTime)
          throw new BusinessException("Phiếu Học Phí Trọn Gói Của Học Viên Này Đã Tồn Tại");
        if (classId.HasValue && await db.TuitionFees.AnyAsync(f => f.StudentId == input.StudentId && f.ClassRoomId == classId.Value && f.FeeType == FeeType.Monthly && f.Id != input.Id))
          throw new BusinessException("Học Viên Đã Có Phiếu Học Phí Theo Tháng Cho Lớp Học Này");
      }
      else
      {
        bool hasOneTime = classId.HasValue
          ? await db.TuitionFees.AnyAsync(f => f.StudentId == input.StudentId && (f.ClassRoomId == classId.Value || f.ClassRoomId == null) && f.FeeType == FeeType.OneTime && f.Id != input.Id)
          : await db.TuitionFees.AnyAsync(f => f.StudentId == input.StudentId && f.FeeType == FeeType.OneTime && f.Id != input.Id);
        if (hasOneTime)
          throw new BusinessException("Học Viên Đã Đóng Học Phí Trọn Gói");
        bool hasMonthly = classId.HasValue
          ? await db.TuitionFees.AnyAsync(f => f.StudentId == input.StudentId && (f.ClassRoomId == classId.Value || f.ClassRoomId == null) && f.FeeType == FeeType.Monthly && f.Month == input.Month && f.Year == input.Year && f.Id != input.Id)
          : await db.TuitionFees.AnyAsync(f => f.StudentId == input.StudentId && f.FeeType == FeeType.Monthly && f.Month == input.Month && f.Year == input.Year && f.Id != input.Id);
        if (hasMonthly)
          throw new BusinessException($"Phiếu Học Phí Tháng {input.Month:D2}/{input.Year} Của Học Viên Này Đã Tồn Tại");
      }
      TuitionFee fee;
      if (input.Id == 0)
      {
        fee = new TuitionFee();
        db.TuitionFees.Add(fee);
      }
      else
      {
        fee = await db.TuitionFees.FindAsync(input.Id) ?? throw new BusinessException("Không Tìm Thấy Phiếu Học Phí");
      }
      fee.StudentId = input.StudentId;
      fee.ClassRoomId = input.ClassRoomId > 0 ? input.ClassRoomId : null;
      fee.FeeType = input.FeeType;
      fee.Month = input.FeeType == FeeType.Monthly ? input.Month : DateTime.Today.Month;
      fee.Year = input.FeeType == FeeType.Monthly ? input.Year : DateTime.Today.Year;
      fee.Amount = input.Amount;
      fee.Note = input.Note?.Trim();
      await db.SaveChangesAsync();
      Audit(db, input.Id == 0 ? "TuitionFee.Create" : "TuitionFee.Update", "TuitionFee", fee.Id, $"{fee.PeriodText}: {fee.Amount:N0} đ");
      if (fee.ClassRoomId.HasValue)
      {
        var enrollment = await db.Enrollments.FirstOrDefaultAsync(e => e.StudentId == fee.StudentId && e.ClassRoomId == fee.ClassRoomId.Value);
        if (enrollment == null)
        {
          enrollment = new Enrollment
          {
            StudentId = fee.StudentId,
            ClassRoomId = fee.ClassRoomId.Value,
            Status = EnrollmentStatus.Active,
            TuitionFee = fee.Amount,
            EnrolledAt = DateTime.Now
          };
          db.Enrollments.Add(enrollment);
        }
        else if (enrollment.Status == EnrollmentStatus.Dropped)
        {
          enrollment.Status = EnrollmentStatus.Active;
        }
        await db.SaveChangesAsync();
      }
      return fee;
    }
    public async Task TogglePaidAsync(int feeId)
    {
      var current = Session.RequireUser();
      if (current.Role != UserRole.Admin)
        throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Học Phí");
      using var db = CreateContext();
      var fee = await db.TuitionFees.FindAsync(feeId) ?? throw new BusinessException("Không Tìm Thấy Phiếu Học Phí");
      fee.IsPaid = !fee.IsPaid;
      fee.PaidAt = fee.IsPaid ? DateTime.Now : null;
      Audit(db, "TuitionFee.TogglePaid", "TuitionFee", fee.Id, fee.IsPaid ? "Đã Đóng" : "Chưa Đóng");
      await db.SaveChangesAsync();
    }
    public async Task DeleteTuitionFeeAsync(int feeId)
    {
      var current = Session.RequireUser();
      if (current.Role != UserRole.Admin)
        throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Học Phí");
      using var db = CreateContext();
      var fee = await db.TuitionFees.FindAsync(feeId) ?? throw new BusinessException("Không Tìm Thấy Phiếu Học Phí");
      int? classId = fee.ClassRoomId;
      int studentId = fee.StudentId;
      db.TuitionFees.Remove(fee);
      Audit(db, "TuitionFee.Delete", "TuitionFee", fee.Id, $"{fee.PeriodText}");
      await db.SaveChangesAsync();
      if (classId.HasValue && !await db.TuitionFees.AnyAsync(f => f.StudentId == studentId && f.ClassRoomId == classId.Value))
      {
        var enrollment = await db.Enrollments.Include(e => e.Attendances).FirstOrDefaultAsync(e => e.StudentId == studentId && e.ClassRoomId == classId.Value);
        if (enrollment != null && !enrollment.Attendances.Any())
        {
          db.Enrollments.Remove(enrollment);
          await db.SaveChangesAsync();
        }
      }
    }
  }
}