using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using EduFlow.Models.Common;
using EduFlow.Models.Enums;
namespace EduFlow.Models.Entities
{
  public class ClassRoom : BaseEntity, IVersioned
  {
    public string Code { get; set; } = string.Empty;
    public int CourseId { get; set; }
    public Course? Course { get; set; }
    public int TeacherId { get; set; }
    public User? Teacher { get; set; }
    public int PhysicalRoomId { get; set; }
    public Room? PhysicalRoom { get; set; }
    public DateTime StartDate { get; set; }
    public WeekDays Days { get; set; }
    [NotMapped]
    public TimeSpan StartTime
    {
      get => TimeSpan.TryParse(StartTimeText, out var t) ? t : TimeSpan.Zero;
      set => StartTimeText = value.ToString(@"hh\:mm\:ss");
    }
    [NotMapped]
    public TimeSpan EndTime
    {
      get => TimeSpan.TryParse(EndTimeText, out var t) ? t : TimeSpan.Zero;
      set => EndTimeText = value.ToString(@"hh\:mm\:ss");
    }
    public string StartTimeText { get; set; } = "00:00:00";
    public string EndTimeText { get; set; } = "00:00:00";
    public int MinStudents { get; set; } = 5;
    public int MaxStudents { get; set; } = 20;
    public ClassStatus Status { get; set; } = ClassStatus.Open;
    public string? CancelReason { get; set; }
    public Guid RowVersion { get; set; } = Guid.NewGuid();
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Enrollment> Enrollments { get; set; } = new List<Enrollment>();
    public int ActiveStudentCount => Enrollments.Count(e => e.HoldsSeat);
    public bool IsFull => ActiveStudentCount >= MaxStudents;
    public bool AcceptsEnrollment => Status is ClassStatus.Open or ClassStatus.InProgress;
    public string ScheduleText => $"{Days.ToShortText()} | {StartTime:hh\\:mm} - {EndTime:hh\\:mm}";
    public override string ToString() => $"{Code} - {Course?.Name}";
    public static bool CanMove(ClassStatus from, ClassStatus to) => (from, to) switch
    {
      (ClassStatus.Open, ClassStatus.InProgress) => true,
      (ClassStatus.Open, ClassStatus.Cancelled) => true,
      (ClassStatus.InProgress, ClassStatus.Finished) => true,
      _ => false
    };
  }
}