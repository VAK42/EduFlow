using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services;
using EduFlow.Services.Common;
namespace EduFlow.WPF.ViewModels
{
  public partial class SalaryViewModel : ViewModelBase
  {
    private readonly ISalaryService _salaryService;
    private readonly UserSession _session;
    public ObservableCollection<Salary> Salaries { get; } = new();
    public ObservableCollection<User> EligibleUsers { get; } = new();
    public bool IsAdmin => _session.CurrentUser?.Role == UserRole.Admin;
    public bool IsNotAdmin => !IsAdmin;
    [ObservableProperty] private DateTime? _filterDate = DateTime.Today;
    [ObservableProperty] private DateTime? _editPeriodDate = DateTime.Today;
    [ObservableProperty] private Salary? _selectedSalary;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private int _editId;
    [ObservableProperty] private int _editUserId;
    [ObservableProperty] private decimal _baseSalary;
    [ObservableProperty] private decimal _allowance;
    [ObservableProperty] private decimal _bonus;
    [ObservableProperty] private decimal _deduction;
    [ObservableProperty] private string? _note;
    public string FormTitle => EditId == 0 ? "Thêm Phiếu Lương" : "Chỉnh Sửa Phiếu Lương";
    public override string Title => IsAdmin ? "Quản Lý Lương Nhân Viên & Giáo Viên" : "Bảng Lương Hàng Tháng";
    public SalaryViewModel(ISalaryService salaryService, UserSession session, IDialogService dialog) : base(dialog)
    {
      _salaryService = salaryService;
      _session = session;
    }
    public override Task LoadAsync() => RunAsync(async () =>
    {
      OnPropertyChanged(nameof(IsAdmin));
      OnPropertyChanged(nameof(IsNotAdmin));
      OnPropertyChanged(nameof(Title));
      if (IsAdmin)
      {
        EligibleUsers.Clear();
        foreach (var u in await _salaryService.GetEligibleUsersAsync()) EligibleUsers.Add(u);
      }
      await ReloadSalariesAsync();
    });
    partial void OnFilterDateChanged(DateTime? value) => _ = ReloadSalariesAsync();
    partial void OnEditIdChanged(int value) => OnPropertyChanged(nameof(FormTitle));
    private async Task ReloadSalariesAsync()
    {
      Salaries.Clear();
      int? m = FilterDate?.Month;
      int? y = FilterDate?.Year;
      foreach (var s in await _salaryService.GetSalariesAsync(m, y, null))
        Salaries.Add(s);
    }
    [RelayCommand]
    private void ClearFilterDate()
    {
      FilterDate = null;
    }
    [RelayCommand]
    private void New()
    {
      if (!IsAdmin) return;
      EditId = 0;
      EditUserId = EligibleUsers.FirstOrDefault()?.Id ?? 0;
      EditPeriodDate = FilterDate ?? DateTime.Today;
      BaseSalary = 0;
      Allowance = 0;
      Bonus = 0;
      Deduction = 0;
      Note = null;
      IsEditing = true;
    }
    [RelayCommand]
    private void Edit(Salary? target = null)
    {
      if (!IsAdmin) return;
      var s = target ?? SelectedSalary;
      if (s == null) return;
      SelectedSalary = s;
      EditId = s.Id;
      EditUserId = s.UserId;
      EditPeriodDate = new DateTime(s.Year, s.Month, 1);
      BaseSalary = s.BaseSalary;
      Allowance = s.Allowance;
      Bonus = s.Bonus;
      Deduction = s.Deduction;
      Note = s.Note;
      IsEditing = true;
    }
    [RelayCommand]
    private void CancelEdit()
    {
      IsEditing = false;
    }
    [RelayCommand]
    private Task SaveAsync() => RunAsync(async () =>
    {
      if (!IsAdmin) return;
      int m = EditPeriodDate?.Month ?? DateTime.Today.Month;
      int y = EditPeriodDate?.Year ?? DateTime.Today.Year;
      var salary = new Salary
      {
        Id = EditId,
        UserId = EditUserId,
        Month = m,
        Year = y,
        BaseSalary = BaseSalary,
        Allowance = Allowance,
        Bonus = Bonus,
        Deduction = Deduction,
        Note = Note
      };
      await _salaryService.SaveSalaryAsync(salary);
      Dialog.ShowInfo($"Đã Lưu Phiếu Lương Tháng {salary.Month:D2}/{salary.Year}");
      IsEditing = false;
      await ReloadSalariesAsync();
    });
    [RelayCommand]
    private Task TogglePaidAsync(Salary? target = null) => RunAsync(async () =>
    {
      if (!IsAdmin) return;
      var s = target ?? SelectedSalary;
      if (s == null) return;
      string action = s.IsPaid ? "Hủy Xác Nhận Đã Thanh Toán" : "Xác Nhận Đã Thanh Toán";
      if (!Dialog.Confirm($"{action} Lương Tháng {s.Month:D2}/{s.Year} Cho {s.User?.FullName}?")) return;
      await _salaryService.TogglePaidAsync(s.Id);
      await ReloadSalariesAsync();
    });
    [RelayCommand]
    private Task DeleteAsync(Salary? target = null) => RunAsync(async () =>
    {
      if (!IsAdmin) return;
      var s = target ?? SelectedSalary;
      if (s == null) return;
      if (!Dialog.Confirm($"Xác Nhận Xóa Phiếu Lương Tháng {s.Month:D2}/{s.Year} Của {s.User?.FullName}?")) return;
      await _salaryService.DeleteSalaryAsync(s.Id);
      Dialog.ShowInfo("Đã Xóa Phiếu Lương Thành Công");
      if (EditId == s.Id) IsEditing = false;
      await ReloadSalariesAsync();
    });
  }
}