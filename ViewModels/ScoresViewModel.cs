using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services;
using EduFlow.Services.Common;
using EduFlow.Services.Dtos;
namespace EduFlow.WPF.ViewModels;
public partial class ScoresViewModel : ViewModelBase
{
  private readonly IClassService _classes;
  private readonly IScoreService _scores;
  private readonly ISettingsService _settings;
  public ObservableCollection<ClassRoom> Classes { get; } = new();
  public ObservableCollection<ScoreRow> Rows { get; } = new();
  [ObservableProperty]
  [NotifyCanExecuteChangedFor(nameof(SaveCommand))]
  private ClassRoom? _selectedClass;
  [ObservableProperty] private string _ruleText = "";
  public bool CanSave { get; }
  public override string Title => "Bảng Điểm";
  public ScoresViewModel(IClassService classes, IScoreService scores, ISettingsService settings,
    UserSession session, IDialogService dialog) : base(dialog)
  {
    _classes = classes;
    _scores = scores;
    _settings = settings;
    CanSave = session.Can(Permission.EnterScores);
  }
  public override Task LoadAsync() => RunAsync(async () =>
  {
    var rule = await _settings.GetPassRuleAsync();
    RuleText = $"Điểm Tổng = 0,4 × Quá Trình + 0,6 × Cuối Kỳ · Làm Tròn 2 Chữ Số · Đạt Khi Điểm Tổng ≥ {rule.PassScore:0.##} & Chuyên Cần ≥ {rule.MinAttendancePercent:0.#}%";
    Classes.Clear();
    foreach (var c in await _classes.GetMyClassesAsync())
      if (c.Status is ClassStatus.InProgress or ClassStatus.Finished) Classes.Add(c);
  });
  partial void OnSelectedClassChanged(ClassRoom? value) => _ = RunAsync(LoadRowsAsync);
  private async Task LoadRowsAsync()
  {
    Rows.Clear();
    if (SelectedClass != null)
      foreach (var r in await _scores.GetSheetAsync(SelectedClass.Id)) Rows.Add(r);
    SaveCommand.NotifyCanExecuteChanged();
  }
  private bool CanSaveScores() => CanSave && SelectedClass != null && Rows.Count > 0;
  [RelayCommand(CanExecute = nameof(CanSaveScores))]
  private Task SaveAsync() => RunAsync(async () =>
  {
    if (SelectedClass == null) throw new BusinessException("Hãy Chọn Lớp");
    foreach (var r in Rows)
    {
      if (!r.ProcessScore.HasValue) throw new BusinessException($"Vui Lòng Nhập Điểm Quá Trình Cho {r.StudentName}");
      if (!r.FinalScore.HasValue) throw new BusinessException($"Vui Lòng Nhập Điểm Cuối Kỳ Cho {r.StudentName}");
      if (r.ProcessScore.Value < 0 || r.ProcessScore.Value > 10) throw new BusinessException($"Điểm Quá Trình Của {r.StudentName} Phải Từ 0 Đến 10");
      if (r.FinalScore.Value < 0 || r.FinalScore.Value > 10) throw new BusinessException($"Điểm Cuối Kỳ Của {r.StudentName} Phải Từ 0 Đến 10");
    }
    if (SelectedClass.Status == ClassStatus.Finished
      && !Dialog.Confirm("Lớp Đã Kết Thúc. Sửa Điểm Sẽ Xét Lại Kết Quả Đạt / Không Đạt & Được Ghi Vào Nhật Ký. Tiếp Tục?"))
      return;
    await _scores.SaveAsync(SelectedClass.Id, Rows);
    Dialog.ShowInfo($"Đã Lưu Bảng Điểm Lớp {SelectedClass.Code}");
    await LoadRowsAsync();
  });
}