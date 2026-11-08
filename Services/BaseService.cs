using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;
using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
namespace EduFlow.Services.Common
{
  public abstract class BaseService
  {
    private readonly IEduFlowDbContextFactory _contextFactory;
    protected UserSession Session { get; }
    protected BaseService(IEduFlowDbContextFactory contextFactory, UserSession session)
    {
      _contextFactory = contextFactory;
      Session = session;
    }
    protected EduFlowDbContext CreateContext() => _contextFactory.CreateDbContext();
    protected void Audit(EduFlowDbContext db, string action, string entity, int? entityId, string? detail = null)
    {
      db.AuditLogs.Add(new AuditLog
      {
        UserId = Session.CurrentUser?.Id,
        Action = action,
        Entity = entity,
        EntityId = entityId,
        Detail = detail
      });
    }
    protected static async Task SaveWithConcurrencyAsync(EduFlowDbContext db, string conflictMessage)
    {
      try
      {
        await db.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        throw new BusinessException(conflictMessage);
      }
    }
  }
}