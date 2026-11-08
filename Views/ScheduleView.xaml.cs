using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
namespace EduFlow.WPF.Views
{
  public partial class ScheduleView : UserControl
  {
    public ScheduleView()
    {
      InitializeComponent();
      Loaded += OnLoaded;
    }
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      AttachWeekFormatter(FilterDatePicker);
    }
    private void AttachWeekFormatter(DatePicker? dp)
    {
      if (dp == null) return;
      dp.Loaded += (s, args) => FormatWeek(dp);
      dp.SelectedDateChanged += (s, args) => FormatWeek(dp);
      dp.CalendarClosed += (s, args) => FormatWeek(dp);
      FormatWeek(dp);
      dp.Dispatcher.BeginInvoke(new Action(() =>
      {
        if (dp.Template?.FindName("PART_TextBox", dp) is DatePickerTextBox tb)
        {
          tb.LostFocus += (s, args) =>
          {
            var text = tb.Text?.Trim() ?? "";
            if (text.StartsWith("Tuần", StringComparison.OrdinalIgnoreCase))
              text = text.Substring(4).Trim();
            if (int.TryParse(text, out var weekNum) && weekNum >= 1 && weekNum <= 53)
            {
              var jan1 = new DateTime(DateTime.Today.Year, 1, 1);
              var daysOffset = DayOfWeek.Thursday - jan1.DayOfWeek;
              var firstThursday = jan1.AddDays(daysOffset);
              var cal = CultureInfo.InvariantCulture.Calendar;
              var firstWeek = cal.GetWeekOfYear(firstThursday, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
              var weekNumTarget = weekNum;
              if (firstWeek == 1) weekNumTarget -= 1;
              var result = firstThursday.AddDays(weekNumTarget * 7);
              dp.SelectedDate = result.AddDays(-3);
            }
            else if (DateTime.TryParse(tb.Text, out var parsed))
            {
              dp.SelectedDate = parsed;
            }
            FormatWeek(dp);
          };
        }
      }));
    }
    private static int GetWeek(DateTime dt)
    {
      return CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(
        dt, CalendarWeekRule.FirstFourDayWeek, DayOfWeek.Monday);
    }
    private static void FormatWeek(DatePicker dp)
    {
      dp.Dispatcher.BeginInvoke(new Action(() =>
      {
        if (dp.Template?.FindName("PART_TextBox", dp) is not DatePickerTextBox tb) return;
        if (!dp.SelectedDate.HasValue)
        {
          tb.Text = string.Empty;
        }
        else
        {
          var dt = dp.SelectedDate.Value;
          tb.Text = $"Tuần {GetWeek(dt)}";
        }
      }));
    }
  }
}