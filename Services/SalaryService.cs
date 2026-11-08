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
  public interface ISalaryService
  {
    Task<List<Salary>> GetSalariesAsync(int? month, int? year, int? userId);
    Task<List<User>> GetEligibleUsersAsync();
    Task<Salary> SaveSalaryAsync(Salary input);
    Task TogglePaidAsync(int salaryId);
    Task DeleteSalaryAsync(int salaryId);
  }
  public class SalaryService : BaseService, ISalaryService
  {
    public SalaryService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
    public async Task<List<Salary>> GetSalariesAsync(int? month, int? year, int? userId)
    {
      var current = Session.RequireUser();
      using var db = CreateContext();
      var q = db.Salaries.AsNoTracking().Include(s => s.User).AsQueryable();
      if (current.Role != UserRole.Admin)
      {
        q = q.Where(s => s.UserId == current.Id);
      }
      else if (userId.HasValue && userId.Value > 0)
      {
        q = q.Where(s => s.UserId == userId.Value);
      }
      if (month.HasValue && month.Value > 0)
        q = q.Where(s => s.Month == month.Value);
      if (year.HasValue && year.Value > 0)
        q = q.Where(s => s.Year == year.Value);
      return await q.OrderByDescending(s => s.Year).ThenByDescending(s => s.Month).ThenBy(s => s.User!.FullName).ToListAsync();
    }
    public async Task<List<User>> GetEligibleUsersAsync()
    {
      var current = Session.RequireUser();
      if (current.Role != UserRole.Admin)
        throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Lương");
      using var db = CreateContext();
      return await db.Users.AsNoTracking()
        .Where(u => u.Role != UserRole.Admin && u.IsActive)
        .OrderBy(u => u.FullName)
        .ToListAsync();
    }
    public async Task<Salary> SaveSalaryAsync(Salary input)
    {
      var current = Session.RequireUser();
      if (current.Role != UserRole.Admin)
        throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Lương");
      if (input.UserId <= 0)
        throw new BusinessException("Vui Lòng Chọn Nhân Viên Hoặc Giáo Viên");
      if (input.Month < 1 || input.Month > 12)
        throw new BusinessException("Tháng Không Hợp Lệ");
      if (input.Year < 2000 || input.Year > 2100)
        throw new BusinessException("Năm Không Hợp Lệ");
      if (input.BaseSalary < 0)
        throw new BusinessException("Lương Cơ Bản Phải Lớn Hơn Hoặc Bằng 0");
      if (input.Allowance < 0)
        throw new BusinessException("Phụ Cấp Phải Lớn Hơn Hoặc Bằng 0");
      if (input.Bonus < 0)
        throw new BusinessException("Tiền Thưởng Phải Lớn Hơn Hoặc Bằng 0");
      if (input.Deduction < 0)
        throw new BusinessException("Khấu Trừ Phải Lớn Hơn Hoặc Bằng 0");
      using var db = CreateContext();
      if (await db.Salaries.AnyAsync(s => s.UserId == input.UserId && s.Month == input.Month && s.Year == input.Year && s.Id != input.Id))
        throw new BusinessException($"Phiếu Lương Tháng {input.Month:D2}/{input.Year} Của Nhân Sự Này Đã Tồn Tại");
      Salary salary;
      if (input.Id == 0)
      {
        salary = new Salary();
        db.Salaries.Add(salary);
      }
      else
      {
        salary = await db.Salaries.FindAsync(input.Id) ?? throw new BusinessException("Không Tìm Thấy Phiếu Lương");
      }
      salary.UserId = input.UserId;
      salary.Month = input.Month;
      salary.Year = input.Year;
      salary.BaseSalary = input.BaseSalary;
      salary.Allowance = input.Allowance;
      salary.Bonus = input.Bonus;
      salary.Deduction = input.Deduction;
      salary.CalculateTotal();
      salary.Note = input.Note?.Trim();
      await db.SaveChangesAsync();
      return salary;
    }
    public async Task TogglePaidAsync(int salaryId)
    {
      var current = Session.RequireUser();
      if (current.Role != UserRole.Admin)
        throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Lương");
      using var db = CreateContext();
      var salary = await db.Salaries.FindAsync(salaryId) ?? throw new BusinessException("Không Tìm Thấy Phiếu Lương");
      salary.IsPaid = !salary.IsPaid;
      salary.PaidAt = salary.IsPaid ? DateTime.Now : null;
      await db.SaveChangesAsync();
    }
    public async Task DeleteSalaryAsync(int salaryId)
    {
      var current = Session.RequireUser();
      if (current.Role != UserRole.Admin)
        throw new BusinessException("Chỉ Quản Trị Viên Mới Có Quyền Quản Lý Lương");
      using var db = CreateContext();
      var salary = await db.Salaries.FindAsync(salaryId) ?? throw new BusinessException("Không Tìm Thấy Phiếu Lương");
      db.Salaries.Remove(salary);
      await db.SaveChangesAsync();
    }
  }
}