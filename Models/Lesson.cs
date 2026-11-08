using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using EduFlow.Models.Common;
using EduFlow.Models.Enums;
namespace EduFlow.Models.Entities
{
  public class Lesson : BaseEntity
  {
    public int ClassRoomId { get; set; }
    public ClassRoom? ClassRoom { get; set; }
    public int LessonNo { get; set; }
    public DateTime Date { get; set; }
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
    public int PhysicalRoomId { get; set; }
    public Room? PhysicalRoom { get; set; }
    public int TeacherId { get; set; }
    public User? Teacher { get; set; }
    public LessonStatus Status { get; set; } = LessonStatus.Scheduled;
    public string? Content { get; set; }
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public bool OverlapsWith(Lesson other) =>
      Date.Date == other.Date.Date && StartTime < other.EndTime && other.StartTime < EndTime;
    public override string ToString() => $"Buổi {LessonNo} - {Date:dd/MM/yyyy}";
  }
}