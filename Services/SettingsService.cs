using System.Globalization;
using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
namespace EduFlow.Services;
public record PassRule(decimal PassScore, double MinAttendancePercent);
public interface ISettingsService
{
  Task<PassRule> GetPassRuleAsync();
  Task<List<Setting>> GetAllAsync();
  Task SaveAsync(string key, string value);
  Task<List<AuditLog>> GetAuditLogsAsync(int take = 200);
}
public class SettingsService : BaseService, ISettingsService
{
  public const decimal DefaultPassScore = 5m;
  public const double DefaultMinAttendance = 80;
  public SettingsService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
  public async Task<PassRule> GetPassRuleAsync()
  {
    using var db = CreateContext();
    return await ReadPassRuleAsync(db);
  }
  internal static async Task<PassRule> ReadPassRuleAsync(EduFlowDbContext db)
  {
    var map = await db.Settings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value);
    decimal score = map.TryGetValue(Setting.PassScore, out var s1)
      && decimal.TryParse(s1, NumberStyles.Number, CultureInfo.InvariantCulture, out var v1) ? v1 : DefaultPassScore;
    double att = map.TryGetValue(Setting.MinAttendancePercent, out var s2)
      && double.TryParse(s2, NumberStyles.Number, CultureInfo.InvariantCulture, out var v2) ? v2 : DefaultMinAttendance;
    return new PassRule(score, att);
  }
  public async Task<List<Setting>> GetAllAsync()
  {
    Session.Require(Permission.ManageSettings);
    using var db = CreateContext();
    return await db.Settings.AsNoTracking().OrderBy(s => s.Key).ToListAsync();
  }
  public async Task SaveAsync(string key, string value)
  {
    Session.Require(Permission.ManageSettings);
    if (string.IsNullOrWhiteSpace(value))
      throw new BusinessException("Giá Trị Thiết Lập Không Được Để Trống");
    if (key is Setting.PassScore && (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) || d < 0 || d > 10))
      throw new BusinessException("Điểm Đạt Phải Là Số Từ 0 Đến 10 (Dùng Dấu Chấm, Ví Dụ 5.5)");
    if (key is Setting.MinAttendancePercent && (!double.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var p) || p < 0 || p > 100))
      throw new BusinessException("Tỉ Lệ Chuyên Cần Phải Từ 0 Đến 100");
    using var db = CreateContext();
    var setting = await db.Settings.FindAsync(key);
    if (setting == null)
    {
      setting = new Setting { Key = key };
      db.Settings.Add(setting);
    }
    string old = setting.Value;
    setting.Value = value.Trim();
    Audit(db, "Setting.Update", "Setting", null, $"{key}: {old} -> {setting.Value}");
    await db.SaveChangesAsync();
  }
  public async Task<List<AuditLog>> GetAuditLogsAsync(int take = 200)
  {
    Session.Require(Permission.ManageSettings);
    using var db = CreateContext();
    return await db.AuditLogs.AsNoTracking().Include(a => a.User)
      .OrderByDescending(a => a.Id).Take(take).ToListAsync();
  }
}