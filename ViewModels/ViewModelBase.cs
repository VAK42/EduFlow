using CommunityToolkit.Mvvm.ComponentModel;
using EduFlow.Models.Common;
using EduFlow.Services;
namespace EduFlow.WPF.ViewModels;
public abstract partial class ViewModelBase : ObservableObject
{
  protected IDialogService Dialog { get; }
  [ObservableProperty]
  private bool _isBusy;
  public abstract string Title { get; }
  protected ViewModelBase(IDialogService dialog) => Dialog = dialog;
  public virtual Task LoadAsync() => Task.CompletedTask;
  protected async Task<bool> RunAsync(Func<Task> action)
  {
    if (IsBusy) return false;
    IsBusy = true;
    try
    {
      await action();
      return true;
    }
    catch (BusinessException ex)
    {
      Dialog.ShowWarning(ex.Message);
    }
    catch (Exception ex)
    {
      Dialog.ShowError("Đã Xảy Ra Lỗi Hệ Thống:\n" + (ex.InnerException?.Message ?? ex.Message));
    }
    finally
    {
      IsBusy = false;
    }
    return false;
  }
}