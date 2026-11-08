using System;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.Common;
using System.Data.Entity.ModelConfiguration;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using System.Threading;
using System.Threading.Tasks;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
namespace EduFlow.Data
{
  public interface IEduFlowDbContextFactory
  {
    EduFlowDbContext CreateDbContext();
    string ConnectionString { get; }
  }
  public class EduFlowDbContextFactory : IEduFlowDbContextFactory
  {
    private readonly string _connectionString;
    public EduFlowDbContextFactory(string connectionString) => _connectionString = connectionString;
    public string ConnectionString => _connectionString;
    public EduFlowDbContext CreateDbContext() => new(_connectionString);
  }
  public class EduFlowDbConfiguration : DbConfiguration
  {
    public EduFlowDbConfiguration()
    {
      var providerServices = (DbProviderServices)SQLiteProviderFactory.Instance.GetService(typeof(DbProviderServices));
      SetProviderServices("System.Data.SQLite", providerServices);
      SetProviderServices("System.Data.SQLite.EF6", providerServices);
      SetProviderFactory("System.Data.SQLite", SQLiteFactory.Instance);
      SetProviderFactory("System.Data.SQLite.EF6", SQLiteProviderFactory.Instance);
    }
  }
  [DbConfigurationType(typeof(EduFlowDbConfiguration))]
  public class EduFlowDbContext : DbContext
  {
    public const string DefaultConnectionString =
      "Data Source=EduFlow.db;Version=3;Foreign Keys=True;BinaryGUID=False;";
    static EduFlowDbContext()
    {
      Database.SetInitializer<EduFlowDbContext>(null);
    }
    public EduFlowDbContext() : this(DefaultConnectionString) { }
    public EduFlowDbContext(string connectionString) : base(CreateConnection(connectionString), true) { }
    public EduFlowDbContext(DbConnection existingConnection, bool contextOwnsConnection)
      : base(existingConnection, contextOwnsConnection) { }
    private static DbConnection CreateConnection(string connectionString)
    {
      return new SQLiteConnection(connectionString);
    }
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Student> Students { get; set; } = null!;
    public DbSet<Course> Courses { get; set; } = null!;
    public DbSet<Room> Rooms { get; set; } = null!;
    public DbSet<ClassRoom> Classes { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
    public DbSet<Enrollment> Enrollments { get; set; } = null!;
    public DbSet<Attendance> Attendances { get; set; } = null!;
    public DbSet<Salary> Salaries { get; set; } = null!;
    public DbSet<TuitionFee> TuitionFees { get; set; } = null!;
    protected override void OnModelCreating(DbModelBuilder modelBuilder)
    {
      modelBuilder.Conventions.Remove<PluralizingTableNameConvention>();
      modelBuilder.Conventions.Remove<OneToManyCascadeDeleteConvention>();
      modelBuilder.Conventions.Remove<ManyToManyCascadeDeleteConvention>();
      modelBuilder.Properties<decimal>().Configure(c => c.HasPrecision(18, 0));
      modelBuilder.Configurations.Add(new UserConfiguration());
      modelBuilder.Configurations.Add(new StudentConfiguration());
      modelBuilder.Configurations.Add(new CourseConfiguration());
      modelBuilder.Configurations.Add(new RoomConfiguration());
      modelBuilder.Configurations.Add(new ClassRoomConfiguration());
      modelBuilder.Configurations.Add(new LessonConfiguration());
      modelBuilder.Configurations.Add(new EnrollmentConfiguration());
      modelBuilder.Configurations.Add(new AttendanceConfiguration());
      modelBuilder.Configurations.Add(new SalaryConfiguration());
      modelBuilder.Configurations.Add(new TuitionFeeConfiguration());
    }
    private void StampVersions()
    {
      foreach (var entry in ChangeTracker.Entries<IVersioned>())
      {
        if (entry.State == EntityState.Modified)
          entry.Entity.RowVersion = Guid.NewGuid();
      }
    }
    public override int SaveChanges()
    {
      StampVersions();
      return base.SaveChanges();
    }
    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
      StampVersions();
      return base.SaveChangesAsync(cancellationToken);
    }
  }
  public class UserConfiguration : EntityTypeConfiguration<User>
  {
    public UserConfiguration()
    {
      ToTable("Users");
      Property(x => x.Username).HasMaxLength(50).IsRequired();
      HasIndex(x => x.Username).IsUnique();
      Property(x => x.PasswordHash).HasMaxLength(100).IsRequired();
      Property(x => x.FullName).HasMaxLength(100).IsRequired();
      Property(x => x.Phone).HasMaxLength(15);
      Property(x => x.Email).HasMaxLength(100);
    }
  }
  public class StudentConfiguration : EntityTypeConfiguration<Student>
  {
    public StudentConfiguration()
    {
      ToTable("Students");
      Property(x => x.Code).HasMaxLength(10).IsRequired();
      HasIndex(x => x.Code).IsUnique();
      Property(x => x.FullName).HasMaxLength(100).IsRequired();
      Property(x => x.Phone).HasMaxLength(15).IsRequired();
      Property(x => x.Email).HasMaxLength(100);
      Property(x => x.Address).HasMaxLength(200);
      Property(x => x.Note).HasMaxLength(500);
      Property(x => x.DateOfBirth);
    }
  }
  public class CourseConfiguration : EntityTypeConfiguration<Course>
  {
    public CourseConfiguration()
    {
      ToTable("Courses");
      Property(x => x.Code).HasMaxLength(20).IsRequired();
      HasIndex(x => x.Code).IsUnique();
      Property(x => x.Name).HasMaxLength(150).IsRequired();
      Property(x => x.Description).HasMaxLength(500);
    }
  }
  public class RoomConfiguration : EntityTypeConfiguration<Room>
  {
    public RoomConfiguration()
    {
      ToTable("Rooms");
      Property(x => x.Name).HasMaxLength(50).IsRequired();
      HasIndex(x => x.Name).IsUnique();
    }
  }
  public class ClassRoomConfiguration : EntityTypeConfiguration<ClassRoom>
  {
    public ClassRoomConfiguration()
    {
      ToTable("Classes");
      Property(x => x.Code).HasMaxLength(30).IsRequired();
      HasIndex(x => x.Code).IsUnique();
      Property(x => x.StartDate);
      Property(x => x.CancelReason).HasMaxLength(300);
      Property(x => x.RowVersion).IsConcurrencyToken();
      Ignore(x => x.StartTime);
      Ignore(x => x.EndTime);
      Property(x => x.StartTimeText).HasMaxLength(20);
      Property(x => x.EndTimeText).HasMaxLength(20);
      HasRequired(x => x.Course).WithMany(c => c!.Classes).HasForeignKey(x => x.CourseId);
      HasRequired(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId);
      HasRequired(x => x.PhysicalRoom).WithMany().HasForeignKey(x => x.PhysicalRoomId);
    }
  }
  public class LessonConfiguration : EntityTypeConfiguration<Lesson>
  {
    public LessonConfiguration()
    {
      ToTable("Lessons");
      Property(x => x.Date);
      Property(x => x.Content).HasMaxLength(500);
      Ignore(x => x.StartTime);
      Ignore(x => x.EndTime);
      Property(x => x.StartTimeText).HasMaxLength(20);
      Property(x => x.EndTimeText).HasMaxLength(20);
      HasRequired(x => x.ClassRoom).WithMany(c => c!.Lessons).HasForeignKey(x => x.ClassRoomId);
      HasRequired(x => x.PhysicalRoom).WithMany().HasForeignKey(x => x.PhysicalRoomId);
      HasRequired(x => x.Teacher).WithMany().HasForeignKey(x => x.TeacherId);
      HasIndex(x => new { x.ClassRoomId, x.LessonNo }).IsUnique();
      HasIndex(x => x.Date);
    }
  }
  public class EnrollmentConfiguration : EntityTypeConfiguration<Enrollment>
  {
    public EnrollmentConfiguration()
    {
      ToTable("Enrollments");
      Property(x => x.ProcessScore).HasPrecision(4, 2);
      Property(x => x.FinalScore).HasPrecision(4, 2);
      Property(x => x.TotalScore).HasPrecision(4, 2);
      Property(x => x.RowVersion).IsConcurrencyToken();
      HasRequired(x => x.Student).WithMany(s => s!.Enrollments).HasForeignKey(x => x.StudentId);
      HasRequired(x => x.ClassRoom).WithMany(c => c!.Enrollments).HasForeignKey(x => x.ClassRoomId);
      HasIndex(x => new { x.StudentId, x.ClassRoomId }).IsUnique();
    }
  }
  public class AttendanceConfiguration : EntityTypeConfiguration<Attendance>
  {
    public AttendanceConfiguration()
    {
      ToTable("Attendances");
      Property(x => x.Note).HasMaxLength(200);
      HasIndex(x => new { x.LessonId, x.EnrollmentId }).IsUnique();
      HasRequired(x => x.Lesson).WithMany(s => s!.Attendances).HasForeignKey(x => x.LessonId);
      HasRequired(x => x.Enrollment).WithMany(e => e!.Attendances).HasForeignKey(x => x.EnrollmentId);
    }
  }
  public class SalaryConfiguration : EntityTypeConfiguration<Salary>
  {
    public SalaryConfiguration()
    {
      ToTable("Salaries");
      Property(x => x.Note).HasMaxLength(500);
      HasRequired(x => x.User).WithMany().HasForeignKey(x => x.UserId);
      HasIndex(x => new { x.UserId, x.Month, x.Year });
    }
  }
  public class TuitionFeeConfiguration : EntityTypeConfiguration<TuitionFee>
  {
    public TuitionFeeConfiguration()
    {
      ToTable("TuitionFees");
      Property(x => x.Note).HasMaxLength(500);
      HasRequired(x => x.Student).WithMany().HasForeignKey(x => x.StudentId);
      HasOptional(x => x.ClassRoom).WithMany().HasForeignKey(x => x.ClassRoomId);
      HasIndex(x => new { x.StudentId, x.Month, x.Year });
    }
  }
}