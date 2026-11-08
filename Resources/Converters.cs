using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using EduFlow.Models.Enums;
namespace EduFlow.WPF.Converters;
public class EnumTextConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
    value is Enum e ? e.ToText() : value?.ToString()!;
  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}
public class NullToCollapsedConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
    value == null ? Visibility.Collapsed : Visibility.Visible;
  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}
public class NullToVisibleConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
    value == null ? Visibility.Visible : Visibility.Collapsed;
  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}
public class ZeroToVisibleConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
    value is int n && n == 0 ? Visibility.Visible : Visibility.Collapsed;
  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}
public class InverseBoolConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b && !b;
  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => value is bool b && !b;
}
public class InitialsConverter : IValueConverter
{
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var parts = (value as string ?? "").Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length == 0) return "?";
    if (parts.Length == 1) return parts[0].Substring(0, 1).ToUpper();
    return (parts[0].Substring(0, 1) + parts[parts.Length - 1].Substring(0, 1)).ToUpper();
  }
  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}
public class StatusBrushConverter : IValueConverter
{
  public string Part { get; set; } = "Foreground";
  private enum Tone { Green, Blue, Amber, Red, Gray, Violet }
  private static readonly Dictionary<Tone, (string Fg, string Bg)> Palette = new()
  {
    [Tone.Green] = ("#15803D", "#DCFCE7"),
    [Tone.Blue] = ("#1D4ED8", "#DBEAFE"),
    [Tone.Amber] = ("#B45309", "#FEF3C7"),
    [Tone.Red] = ("#B91C1C", "#FEE2E2"),
    [Tone.Gray] = ("#475569", "#F1F5F9"),
    [Tone.Violet] = ("#6D28D9", "#EDE9FE"),
  };
  private static Tone ToneOf(object? value) => value switch
  {
    ClassStatus.Open => Tone.Violet,
    ClassStatus.InProgress => Tone.Blue,
    ClassStatus.Finished => Tone.Gray,
    ClassStatus.Cancelled => Tone.Red,
    PaymentStatus.Paid => Tone.Green,
    PaymentStatus.Partial => Tone.Amber,
    PaymentStatus.Unpaid => Tone.Red,
    AttendanceStatus.Present => Tone.Green,
    AttendanceStatus.Late => Tone.Amber,
    AttendanceStatus.AbsentExcused => Tone.Blue,
    AttendanceStatus.AbsentUnexcused => Tone.Red,
    LessonStatus.Done => Tone.Green,
    LessonStatus.Scheduled => Tone.Gray,
    LessonStatus.Cancelled => Tone.Red,
    EnrollmentStatus.Active => Tone.Blue,
    EnrollmentStatus.Completed => Tone.Green,
    EnrollmentStatus.Reserved => Tone.Amber,
    EnrollmentStatus.Dropped => Tone.Gray,
    EnrollmentStatus.Failed => Tone.Red,
    CourseCategory.Language => Tone.Blue,
    CourseCategory.Computer => Tone.Violet,
    CourseCategory.ShortCourse => Tone.Amber,
    LeadStatus.New => Tone.Violet,
    LeadStatus.Contacted => Tone.Blue,
    LeadStatus.TrialScheduled => Tone.Amber,
    LeadStatus.TrialDone => Tone.Amber,
    LeadStatus.Converted => Tone.Green,
    LeadStatus.Rejected => Tone.Gray,
    ReceiptStatus.Valid => Tone.Green,
    ReceiptStatus.Cancelled => Tone.Red,
    ReductionStatus.Pending => Tone.Amber,
    ReductionStatus.Approved => Tone.Green,
    ReductionStatus.Rejected => Tone.Red,
    UserRole.Admin => Tone.Violet,
    UserRole.Staff => Tone.Blue,
    UserRole.Teacher => Tone.Green,
    FeeType.Monthly => Tone.Blue,
    FeeType.OneTime => Tone.Violet,
    true => Tone.Green,
    false => Tone.Gray,
    _ => Tone.Gray
  };
  public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
  {
    var (fg, bg) = Palette[ToneOf(value)];
    var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(Part == "Background" ? bg : fg));
    brush.Freeze();
    return brush;
  }
  public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
    throw new NotSupportedException();
}