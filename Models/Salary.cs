using System;
using EduFlow.Models.Common;
using EduFlow.Models.Enums;
namespace EduFlow.Models.Entities
{
  public class Salary : BaseEntity
  {
    public int UserId { get; set; }
    public User? User { get; set; }
    public int Month { get; set; } = DateTime.Today.Month;
    public int Year { get; set; } = DateTime.Today.Year;
    public decimal BaseSalary { get; set; }
    public decimal Allowance { get; set; }
    public decimal Bonus { get; set; }
    public decimal Deduction { get; set; }
    public decimal TotalSalary { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }
    public string PeriodText => $"Tháng {Month:D2}/{Year}";
    public string StatusText => IsPaid ? "Đã Thanh Toán" : "Chưa Thanh Toán";
    public void CalculateTotal()
    {
      TotalSalary = Math.Max(0m, BaseSalary + Allowance + Bonus - Deduction);
    }
  }
}