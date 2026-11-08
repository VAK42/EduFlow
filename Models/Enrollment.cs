using System;
using System.Collections.Generic;
using System.Linq;
using EduFlow.Models.Common;
using EduFlow.Models.Enums;
namespace EduFlow.Models.Entities
{
  public class Enrollment : BaseEntity, IVersioned
  {
    public const decimal ProcessWeight = 0.4m;
    public const decimal FinalWeight = 0.6m;
    public int StudentId { get; set; }
    public Student? Student { get; set; }
    public int ClassRoomId { get; set; }
    public ClassRoom? ClassRoom { get; set; }
    public DateTime EnrolledAt { get; set; } = DateTime.Now;
    public decimal TuitionFee { get; set; }
    public EnrollmentStatus Status { get; set; } = EnrollmentStatus.Active;
    public decimal? ProcessScore { get; set; }
    public decimal? FinalScore { get; set; }
    public decimal? TotalScore { get; set; }
    public Guid RowVersion { get; set; } = Guid.NewGuid();
    public ICollection<Attendance> Attendances { get; set; } = new List<Attendance>();
    public bool HoldsSeat => Status is EnrollmentStatus.Active or EnrollmentStatus.Reserved;
    public int AttendedCount => Attendances.Count(a => a.IsAttended);
    public int TotalAttendedSessions => Attendances.Count;
    public static double AttendanceRate(int attended, int total) => total == 0 ? 0 : Math.Round((double)attended / total * 100, 1);
    public static bool IsPassed(decimal totalScore, double attendanceRate, decimal passScore, double minAttendancePercent) => totalScore >= passScore && attendanceRate >= minAttendancePercent;
    public void SetScores(decimal? processScore, decimal? finalScore)
    {
      if (processScore.HasValue && (processScore < 0 || processScore > 10))
        throw new BusinessException("Điểm Quá Trình Phải Từ 0 Đến 10");
      if (finalScore.HasValue && (finalScore < 0 || finalScore > 10))
        throw new BusinessException("Điểm Cuối Kỳ Phải Từ 0 Đến 10");
      ProcessScore = processScore;
      FinalScore = finalScore;
      CalculateTotalScore();
    }
    public void CalculateTotalScore()
    {
      if (ProcessScore.HasValue && FinalScore.HasValue)
        TotalScore = Math.Round(ProcessScore.Value * ProcessWeight + FinalScore.Value * FinalWeight, 2);
      else
        TotalScore = null;
    }
  }
}