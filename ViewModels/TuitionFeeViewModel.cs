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
  public partial class TuitionFeeViewModel : ViewModelBase
  {
    private readonly ITuitionFeeService _service;
    private readonly UserSession _session;
    public ObservableCollection<TuitionFee> TuitionFees { get; } = new();
    public ObservableCollection<Student> EligibleStudents { get; } = new();
    public ObservableCollection<ClassRoom> EligibleClasses { get; } = new();
    public FeeType[] FeeTypes { get; } = (FeeType[])Enum.GetValues(typeof(FeeType));
    [ObservableProperty] private DateTime? _filterDate = DateTime.Today;
    [ObservableProperty] private FeeType? _filterFeeType;
    [ObservableProperty] private bool? _filterPaidStatus;
    [ObservableProperty] private string? _searchKeyword;
    [ObservableProperty] private TuitionFee? _selectedFee;
    [ObservableProperty] private bool _isEditing;
    [ObservableProperty] private int _editId;
    [ObservableProperty] private int _editStudentId;
    [ObservableProperty] private int? _editClassRoomId;
    [ObservableProperty] private FeeType _editFeeType = FeeType.Monthly;
    [ObservableProperty] private DateTime? _editPeriodDate = DateTime.Today;
    [ObservableProperty] private decimal _editAmount;
    [ObservableProperty] private string? _editNote;
    public bool IsMonthly => EditFeeType == FeeType.Monthly;
    public string FormTitle => EditId == 0 ? "Thêm Học Phí" : "Chỉnh Sửa Học Phí";
    public override string Title => "Quản Lý Học Phí Học Viên";
    public TuitionFeeViewModel(ITuitionFeeService service, UserSession session, IDialogService dialog) : base(dialog)
    {
      _service = service;
      _session = session;
    }
    public override Task LoadAsync() => RunAsync(async () =>
    {
      EligibleStudents.Clear();
      foreach (var s in await _service.GetEligibleStudentsAsync()) EligibleStudents.Add(s);
      EligibleClasses.Clear();
      foreach (var c in await _service.GetEligibleClassesAsync()) EligibleClasses.Add(c);
      await ReloadFeesAsync();
    });
    partial void OnFilterDateChanged(DateTime? value) => _ = ReloadFeesAsync();
    partial void OnFilterFeeTypeChanged(FeeType? value) => _ = ReloadFeesAsync();
    partial void OnFilterPaidStatusChanged(bool? value) => _ = ReloadFeesAsync();
    partial void OnSearchKeywordChanged(string? value) => _ = ReloadFeesAsync();
    partial void OnEditClassRoomIdChanged(int? value) => UpdateLockedFee();
    partial void OnEditFeeTypeChanged(FeeType value)
    {
      OnPropertyChanged(nameof(IsMonthly));
      UpdateLockedFee();
    }
    partial void OnEditIdChanged(int value) => OnPropertyChanged(nameof(FormTitle));
    private void UpdateLockedFee()
    {
      var cls = EligibleClasses.FirstOrDefault(c => c.Id == EditClassRoomId);
      if (cls?.Course != null)
      {
        if (EditFeeType == FeeType.Monthly)
          EditAmount = cls.Course.MonthlyFee > 0 ? cls.Course.MonthlyFee : cls.Course.TuitionFee;
        else
          EditAmount = cls.Course.TuitionFee;
      }
    }
    private async Task ReloadFeesAsync()
    {
      TuitionFees.Clear();
      int? m = FilterDate?.Month;
      int? y = FilterDate?.Year;
      var list = await _service.GetTuitionFeesAsync(m, y, FilterFeeType, FilterPaidStatus, SearchKeyword);
      foreach (var f in list) TuitionFees.Add(f);
    }
    [RelayCommand]
    private void ClearFilterDate()
    {
      FilterDate = null;
    }
    [RelayCommand]
    private void ResetFilters()
    {
      FilterDate = null;
      FilterFeeType = null;
      FilterPaidStatus = null;
      SearchKeyword = null;
    }
    [RelayCommand]
    private void New()
    {
      EditId = 0;
      EditStudentId = EligibleStudents.FirstOrDefault()?.Id ?? 0;
      EditClassRoomId = EligibleClasses.FirstOrDefault()?.Id;
      EditFeeType = FeeType.Monthly;
      EditPeriodDate = FilterDate ?? DateTime.Today;
      UpdateLockedFee();
      EditNote = null;
      IsEditing = true;
    }
    [RelayCommand]
    private void Edit(TuitionFee? target = null)
    {
      var f = target ?? SelectedFee;
      if (f == null) return;
      SelectedFee = f;
      EditId = f.Id;
      EditStudentId = f.StudentId;
      EditClassRoomId = f.ClassRoomId;
      EditFeeType = f.FeeType;
      EditPeriodDate = new DateTime(f.Year, f.Month, 1);
      EditAmount = f.Amount;
      EditNote = f.Note;
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
      int m = EditPeriodDate?.Month ?? DateTime.Today.Month;
      int y = EditPeriodDate?.Year ?? DateTime.Today.Year;
      var fee = new TuitionFee
      {
        Id = EditId,
        StudentId = EditStudentId,
        ClassRoomId = EditClassRoomId,
        FeeType = EditFeeType,
        Month = m,
        Year = y,
        Amount = EditAmount,
        Note = EditNote
      };
      await _service.SaveTuitionFeeAsync(fee);
      Dialog.ShowInfo($"Đã Lưu Phiếu Học Phí {fee.PeriodText}");
      IsEditing = false;
      await ReloadFeesAsync();
    });
    [RelayCommand]
    private Task TogglePaidAsync(TuitionFee? target = null) => RunAsync(async () =>
    {
      var f = target ?? SelectedFee;
      if (f == null) return;
      string action = f.IsPaid ? "Hủy Xác Nhận Đã Đóng" : "Xác Nhận Đã Đóng";
      if (!Dialog.Confirm($"{action} Học Phí Cho {f.Student?.FullName}?")) return;
      await _service.TogglePaidAsync(f.Id);
      await ReloadFeesAsync();
    });
    [RelayCommand]
    private Task DeleteAsync(TuitionFee? target = null) => RunAsync(async () =>
    {
      var f = target ?? SelectedFee;
      if (f == null) return;
      if (!Dialog.Confirm($"Xác Nhận Xóa Phiếu Học Phí Của {f.Student?.FullName}?")) return;
      await _service.DeleteTuitionFeeAsync(f.Id);
      Dialog.ShowInfo("Đã Xóa Phiếu Học Phí Thành Công");
      if (EditId == f.Id) IsEditing = false;
      await ReloadFeesAsync();
    });
  }
}