using EduFlow.Data;
using EduFlow.Services.Common;
using Microsoft.Extensions.DependencyInjection;
namespace EduFlow.Services;
public static class ServiceRegistration
{
  public static IServiceCollection AddEduFlowServices(this IServiceCollection services, string connectionString)
  {
    services.AddSingleton<IEduFlowDbContextFactory>(new EduFlowDbContextFactory(connectionString));
    services.AddSingleton<UserSession>();
    services.AddSingleton<DatabaseInitializer>();
    services.AddSingleton<IAuthService, AuthService>();
    services.AddSingleton<ICatalogService, CatalogService>();
    services.AddSingleton<IStudentService, StudentService>();
    services.AddSingleton<IUserService, UserService>();
    services.AddSingleton<ISettingsService, SettingsService>();
    services.AddSingleton<IScoreService, ScoreService>();
    services.AddSingleton<ICourseService, CourseService>();
    services.AddSingleton<IClassService, ClassService>();
    services.AddSingleton<IAttendanceService, AttendanceService>();
    services.AddSingleton<IDashboardService, DashboardService>();
    services.AddSingleton<ISalaryService, SalaryService>();
    services.AddSingleton<ITuitionFeeService, TuitionFeeService>();
    return services;
  }
}