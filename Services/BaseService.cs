using System.Data.Entity;
using System.Data.Entity.Infrastructure;
using System.Threading.Tasks;
using EduFlow.Data;
using EduFlow.Models.Common;
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