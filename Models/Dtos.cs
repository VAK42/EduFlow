using System;
using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using EduFlow.Models.Enums;
namespace EduFlow.Services.Dtos
{
  public record DashboardSummary(
    int ActiveStudents,
    int OpenClasses,
    int RunningClasses,
    decimal CollectedThisMonth,
    decimal CollectedLastMonth,
    decimal TotalDebt,
    int DebtorCount,
    bool CanSeeFinance,
    IReadOnlyList<TodayLessonRow> TodayLessons,
    IReadOnlyList<MonthCollection> CollectionByMonth,
    IReadOnlyList<ClassFillRow> ClassFill,
    IReadOnlyList<DebtRow> TopDebts);
  public record MonthCollection(int Year, int Month, decimal Amount);
  public record ClassFillRow(string ClassCode, string CourseName, int Students, int MaxStudents)
  {
    public double Percent => MaxStudents == 0 ? 0 : 100.0 * Students / MaxStudents;
  }
  public record TodayLessonRow(string ClassCode, string CourseName, string TimeText, string RoomName, string TeacherName);
  public record DebtRow(int EnrollmentId, string StudentCode, string StudentName, string Phone,
    string ClassCode, decimal AmountDue, decimal Paid, decimal Debt);
  public class AttendanceRow : ObservableObject
  {
    public int EnrollmentId { get; init; }
    public string StudentCode { get; init; } = "";
    public string StudentName { get; init; } = "";
    public string Phone { get; init; } = "";
    private AttendanceStatus _status = AttendanceStatus.Present;
    public AttendanceStatus Status
    {
      get => _status;
      set => SetProperty(ref _status, value);
    }
    private string? _note;
    public string? Note
    {
      get => _note;
      set => SetProperty(ref _note, value);
    }
    public double AttendanceRate { get; init; }
  }
  public class ScoreRow : ObservableObject
  {
    public int EnrollmentId { get; init; }
    public string StudentCode { get; init; } = "";
    public string StudentName { get; init; } = "";
    public string Phone { get; init; } = "";
    private decimal? _processScore;
    public decimal? ProcessScore
    {
      get => _processScore;
      set
      {
        if (SetProperty(ref _processScore, value))
        {
          _processScoreText = value?.ToString("0.##") ?? "";
          OnPropertyChanged(nameof(ProcessScoreText));
          UpdateTotal();
        }
      }
    }
    private string _processScoreText = "";
    public string ProcessScoreText
    {
      get => _processScoreText;
      set
      {
        if (SetProperty(ref _processScoreText, value))
        {
          if (string.IsNullOrWhiteSpace(value))
          {
            _processScore = null;
          }
          else if (decimal.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
          {
            _processScore = v;
          }
          OnPropertyChanged(nameof(ProcessScore));
          UpdateTotal();
        }
      }
    }
    private decimal? _finalScore;
    public decimal? FinalScore
    {
      get => _finalScore;
      set
      {
        if (SetProperty(ref _finalScore, value))
        {
          _finalScoreText = value?.ToString("0.##") ?? "";
          OnPropertyChanged(nameof(FinalScoreText));
          UpdateTotal();
        }
      }
    }
    private string _finalScoreText = "";
    public string FinalScoreText
    {
      get => _finalScoreText;
      set
      {
        if (SetProperty(ref _finalScoreText, value))
        {
          if (string.IsNullOrWhiteSpace(value))
          {
            _finalScore = null;
          }
          else if (decimal.TryParse(value.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var v))
          {
            _finalScore = v;
          }
          OnPropertyChanged(nameof(FinalScore));
          UpdateTotal();
        }
      }
    }
    private decimal? _totalScore;
    public decimal? TotalScore
    {
      get => _totalScore;
      set => SetProperty(ref _totalScore, value);
    }
    public double AttendanceRate { get; init; }
    private string _resultText = "";
    public string ResultText
    {
      get => _resultText;
      set => SetProperty(ref _resultText, value);
    }
    public decimal PassScore { get; set; } = 5.0m;
    public double MinAttendancePercent { get; set; } = 80.0;
    public void UpdateTotal()
    {
      if (_processScore.HasValue && _finalScore.HasValue)
      {
        TotalScore = Math.Round(_processScore.Value * 0.4m + _finalScore.Value * 0.6m, 2);
        ResultText = TotalScore.Value >= PassScore && AttendanceRate >= MinAttendancePercent
          ? "Dự Kiến Đạt" : "Dự Kiến Không Đạt";
      }
      else
      {
        TotalScore = null;
        ResultText = "Chưa Đủ Điểm";
      }
    }
  }
}