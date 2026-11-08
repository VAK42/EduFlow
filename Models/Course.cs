using System.Collections.Generic;
using EduFlow.Models.Common;
using EduFlow.Models.Enums;
namespace EduFlow.Models.Entities
{
  public class Course : BaseEntity
  {
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public CourseCategory Category { get; set; } = CourseCategory.Language;
    public int SessionCount { get; set; }
    public decimal TuitionFee { get; set; }
    public decimal MonthlyFee { get; set; }
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<ClassRoom> Classes { get; set; } = new List<ClassRoom>();
    public override string ToString() => $"{Code} - {Name}";
  }
}