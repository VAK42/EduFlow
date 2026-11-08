using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Services;
using EduFlow.Services.Common;
using EduFlow.Services.Dtos;
namespace EduFlow.WPF.ViewModels;
public record CollectionBar(string Label, decimal Amount, double BarHeight, bool IsCurrent)
{
  public string AmountText => Amount >= 1_000_000 ? $"{Amount / 1_000_000:0.#} Tr" : Amount > 0 ? $"{Amount / 1000:0} K" : "0";
}
public partial class DashboardViewModel : ViewModelBase
{
  public const double ChartHeight = 160;
  private readonly IDashboardService _dashboard;
  private readonly UserSession _session;
  [ObservableProperty] private DashboardSummary? _summary;
  public ObservableCollection<DebtRow> Debts { get; } = new();
  public ObservableCollection<CollectionBar> CollectionBars { get; } = new();
  public override string Title => "Tổng Quan";
  public string Greeting => $"Xin Chào, {_session.CurrentUser?.FullName}";
  public string CollectionTrend
  {
    get
    {
      if (Summary == null || Summary.CollectedLastMonth == 0) return "Chưa Có Số Liệu Tháng Trước";
      var change = (Summary.CollectedThisMonth - Summary.CollectedLastMonth) / Summary.CollectedLastMonth * 100;
      return $"{(change >= 0 ? "+" : "")}{change:0}% So Với Tháng Trước";
    }
  }
  public DashboardViewModel(IDashboardService dashboard, UserSession session,
    IDialogService dialog) : base(dialog)
  {
    _dashboard = dashboard;
    _session = session;
  }
  [RelayCommand]
  private Task RefreshAsync() => LoadAsync();
  public override Task LoadAsync() => RunAsync(async () =>
  {
    Summary = await _dashboard.GetSummaryAsync();
    OnPropertyChanged(nameof(CollectionTrend));
    CollectionBars.Clear();
    Debts.Clear();
    if (!Summary.CanSeeFinance) return;
    decimal max = Math.Max(1, Summary.CollectionByMonth.Count > 0 ? Summary.CollectionByMonth.Max(m => m.Amount) : 1);
    foreach (var m in Summary.CollectionByMonth)
    {
      bool current = m.Year == DateTime.Today.Year && m.Month == DateTime.Today.Month;
      CollectionBars.Add(new CollectionBar($"T{m.Month}", m.Amount, Math.Max(4, (double)(m.Amount / max) * ChartHeight), current));
    }
    if (Summary.TopDebts != null)
      foreach (var d in Summary.TopDebts) Debts.Add(d);
  });
}