namespace EduFlow.Models.Entities
{
  public class Setting
  {
    public const string PassScore = "PassScore";
    public const string MinAttendancePercent = "MinAttendancePercent";
    public const string CenterName = "CenterName";
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Description { get; set; }
  }
}