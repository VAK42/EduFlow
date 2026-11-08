using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
namespace EduFlow.WPF.Views
{
  public partial class SalaryView : UserControl
  {
    public SalaryView()
    {
      InitializeComponent();
      Loaded += OnLoaded;
    }
    private void OnLoaded(object sender, RoutedEventArgs e)
    {
      AttachMonthFormatter(FilterDatePicker);
      AttachMonthFormatter(EditorDatePicker);
    }
    private void AttachMonthFormatter(DatePicker? dp)
    {
      if (dp == null) return;
      dp.Loaded += (s, args) => FormatMonth(dp);
      dp.SelectedDateChanged += (s, args) => FormatMonth(dp);
      dp.CalendarClosed += (s, args) => FormatMonth(dp);
      FormatMonth(dp);
      dp.Dispatcher.BeginInvoke(new Action(() =>
      {
        if (dp.Template?.FindName("PART_TextBox", dp) is DatePickerTextBox tb)
        {
          tb.LostFocus += (s, args) =>
          {
            if (DateTime.TryParse(tb.Text, out var parsed))
              dp.SelectedDate = parsed;
            FormatMonth(dp);
          };
        }
      }));
    }
    private static void FormatMonth(DatePicker dp)
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
          tb.Text = $"Tháng {dt.Month:D2}/{dt.Year}";
        }
      }));
    }
  }
}