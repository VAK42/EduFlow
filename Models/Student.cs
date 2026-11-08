using System;
using System.Collections.Generic;
using EduFlow.Models.Common;
namespace EduFlow.Models.Entities
{
  public class Student : Person
  {
    public string Code { get; set; } = string.Empty;
    public DateTime? DateOfBirth { get; set; }
    public bool IsMale { get; set; }
    public string? Address { get; set; }
    public string? Note { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public override string RoleName => "Học Viên";
    public override string DisplayName => $"{Code} - {FullName}";
  }
}