using System;
using EduFlow.Models.Common;
using EduFlow.Models.Enums;
namespace EduFlow.Models.Entities
{
  public class TuitionFee : BaseEntity
  {
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int? ClassRoomId { get; set; }
    public ClassRoom? ClassRoom { get; set; }
    public FeeType FeeType { get; set; } = FeeType.Monthly;
    public int Month { get; set; } = DateTime.Today.Month;
    public int Year { get; set; } = DateTime.Today.Year;
    public decimal Amount { get; set; }
    public bool IsPaid { get; set; }
    public DateTime? PaidAt { get; set; }
    public string? Note { get; set; }
    public string PeriodText => FeeType == FeeType.OneTime ? "Trọn Gói" : $"Tháng {Month:D2}/{Year}";
    public string StatusText => IsPaid ? "Đã Đóng" : (IsOverdue ? "Quá Hạn" : "Chưa Đóng");
    public bool IsOverdue => !IsPaid && FeeType == FeeType.Monthly && (Year < DateTime.Today.Year || (Year == DateTime.Today.Year && Month < DateTime.Today.Month));
  }
}