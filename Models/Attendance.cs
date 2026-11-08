using EduFlow.Models.Common;
using EduFlow.Models.Enums;
namespace EduFlow.Models.Entities
{
  public class Attendance : BaseEntity
  {
    public int LessonId { get; set; }
    public Lesson? Lesson { get; set; }
    public int EnrollmentId { get; set; }
    public Enrollment? Enrollment { get; set; }
    public AttendanceStatus Status { get; set; } = AttendanceStatus.Present;
    public string? Note { get; set; }
    public bool IsAttended => Status is AttendanceStatus.Present or AttendanceStatus.Late;
  }
}