using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services.Scheduling;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
namespace EduFlow.Services
{
  public class DatabaseInitializer
  {
    private readonly IEduFlowDbContextFactory _factory;
    public DatabaseInitializer(IEduFlowDbContextFactory factory) => _factory = factory;
    public async Task InitializeAsync()
    {
      CreateSQLiteTables(_factory.ConnectionString);
      using (var db = _factory.CreateDbContext())
      {
        var feesWithClass = await db.TuitionFees.Where(f => f.ClassRoomId != null).ToListAsync();
        foreach (var f in feesWithClass)
        {
          int classId = f.ClassRoomId!.Value;
          bool enrolled = await db.Enrollments.AnyAsync(e => e.StudentId == f.StudentId && e.ClassRoomId == classId);
          if (!enrolled)
          {
            db.Enrollments.Add(new Enrollment
            {
              StudentId = f.StudentId,
              ClassRoomId = classId,
              Status = EnrollmentStatus.Active,
              TuitionFee = f.Amount,
              EnrolledAt = f.CreatedAt
            });
          }
        }
        await db.SaveChangesAsync();
        if (await db.Users.AnyAsync()) return;
        await SeedAsync(db);
      }
    }
    private static void CreateSQLiteTables(string connectionString)
    {
      using (var conn = new SQLiteConnection(connectionString))
      {
        conn.Open();
        using (var pragmaCmd = new SQLiteCommand("PRAGMA journal_mode = WAL; PRAGMA foreign_keys = ON;", conn))
        {
          pragmaCmd.ExecuteNonQuery();
        }
        string[] statements = new string[]
        {
          @"CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreatedAt TEXT NOT NULL,
            Username TEXT NOT NULL UNIQUE,
            PasswordHash TEXT NOT NULL,
            FullName TEXT NOT NULL,
            Phone TEXT,
            Email TEXT,
            Role INTEGER NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1
          );",
          @"CREATE TABLE IF NOT EXISTS Students (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreatedAt TEXT NOT NULL,
            FullName TEXT NOT NULL,
            Phone TEXT NOT NULL,
            Email TEXT,
            Code TEXT NOT NULL UNIQUE,
            DateOfBirth TEXT,
            IsMale INTEGER NOT NULL DEFAULT 1,
            Address TEXT,
            Note TEXT,
            IsActive INTEGER NOT NULL DEFAULT 1
          );",
          @"CREATE TABLE IF NOT EXISTS Courses (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreatedAt TEXT NOT NULL,
            Code TEXT NOT NULL UNIQUE,
            Name TEXT NOT NULL,
            Category INTEGER NOT NULL,
            SessionCount INTEGER NOT NULL,
            TuitionFee NUMERIC NOT NULL,
            MonthlyFee DECIMAL(18,2) NOT NULL DEFAULT 0,
            Description TEXT,
            IsActive INTEGER NOT NULL DEFAULT 1
          );",
          @"CREATE TABLE IF NOT EXISTS Rooms (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreatedAt TEXT NOT NULL,
            Name TEXT NOT NULL UNIQUE,
            Capacity INTEGER NOT NULL,
            IsActive INTEGER NOT NULL DEFAULT 1
          );",
          @"CREATE TABLE IF NOT EXISTS Classes (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreatedAt TEXT NOT NULL,
            Code TEXT NOT NULL UNIQUE,
            CourseId INTEGER NOT NULL,
            TeacherId INTEGER NOT NULL,
            PhysicalRoomId INTEGER NOT NULL,
            StartDate TEXT NOT NULL,
            Days INTEGER NOT NULL,
            StartTimeText TEXT NOT NULL,
            EndTimeText TEXT NOT NULL,
            MinStudents INTEGER NOT NULL DEFAULT 5,
            MaxStudents INTEGER NOT NULL DEFAULT 20,
            Status INTEGER NOT NULL DEFAULT 1,
            CancelReason TEXT,
            RowVersion TEXT NOT NULL,
            FOREIGN KEY (CourseId) REFERENCES Courses(Id),
            FOREIGN KEY (TeacherId) REFERENCES Users(Id),
            FOREIGN KEY (PhysicalRoomId) REFERENCES Rooms(Id)
          );",
          @"CREATE TABLE IF NOT EXISTS Lessons (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreatedAt TEXT NOT NULL,
            ClassRoomId INTEGER NOT NULL,
            LessonNo INTEGER NOT NULL,
            Date TEXT NOT NULL,
            StartTimeText TEXT NOT NULL,
            EndTimeText TEXT NOT NULL,
            PhysicalRoomId INTEGER NOT NULL,
            TeacherId INTEGER NOT NULL,
            Status INTEGER NOT NULL DEFAULT 1,
            Content TEXT,
            FOREIGN KEY (ClassRoomId) REFERENCES Classes(Id),
            FOREIGN KEY (PhysicalRoomId) REFERENCES Rooms(Id),
            FOREIGN KEY (TeacherId) REFERENCES Users(Id)
          );",
          @"CREATE TABLE IF NOT EXISTS Enrollments (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreatedAt TEXT NOT NULL,
            StudentId INTEGER NOT NULL,
            ClassRoomId INTEGER NOT NULL,
            EnrolledAt TEXT NOT NULL,
            TuitionFee NUMERIC NOT NULL,
            Status INTEGER NOT NULL DEFAULT 1,
            ProcessScore NUMERIC,
            FinalScore NUMERIC,
            TotalScore NUMERIC,
            RowVersion TEXT NOT NULL,
            FOREIGN KEY (StudentId) REFERENCES Students(Id),
            FOREIGN KEY (ClassRoomId) REFERENCES Classes(Id)
          );",
          @"CREATE TABLE IF NOT EXISTS Attendances (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreatedAt TEXT NOT NULL,
            LessonId INTEGER NOT NULL,
            EnrollmentId INTEGER NOT NULL,
            Status INTEGER NOT NULL DEFAULT 1,
            Note TEXT,
            FOREIGN KEY (LessonId) REFERENCES Lessons(Id),
            FOREIGN KEY (EnrollmentId) REFERENCES Enrollments(Id)
          );",
          @"CREATE TABLE IF NOT EXISTS Salaries (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreatedAt TEXT NOT NULL,
            UserId INTEGER NOT NULL,
            Month INTEGER NOT NULL,
            Year INTEGER NOT NULL,
            BaseSalary DECIMAL(18,2) NOT NULL DEFAULT 0,
            Allowance DECIMAL(18,2) NOT NULL DEFAULT 0,
            Bonus DECIMAL(18,2) NOT NULL DEFAULT 0,
            Deduction DECIMAL(18,2) NOT NULL DEFAULT 0,
            TotalSalary DECIMAL(18,2) NOT NULL DEFAULT 0,
            IsPaid INTEGER NOT NULL DEFAULT 0,
            PaidAt TEXT,
            Note TEXT,
            FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE
          );",
          @"CREATE TABLE IF NOT EXISTS TuitionFees (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            CreatedAt TEXT NOT NULL,
            StudentId INTEGER NOT NULL,
            ClassRoomId INTEGER,
            FeeType INTEGER NOT NULL DEFAULT 1,
            Month INTEGER,
            Year INTEGER,
            Amount DECIMAL(18,2) NOT NULL DEFAULT 0,
            IsPaid INTEGER NOT NULL DEFAULT 0,
            PaidAt TEXT,
            Note TEXT,
            FOREIGN KEY (StudentId) REFERENCES Students(Id) ON DELETE CASCADE,
            FOREIGN KEY (ClassRoomId) REFERENCES Classes(Id) ON DELETE SET NULL
          );",
          "CREATE UNIQUE INDEX IF NOT EXISTS idx_lessons_class_lessonno ON Lessons(ClassRoomId, LessonNo);",
          "CREATE INDEX IF NOT EXISTS idx_lessons_date ON Lessons(Date);",
          "CREATE UNIQUE INDEX IF NOT EXISTS idx_enrollments_student_class ON Enrollments(StudentId, ClassRoomId);",
          "CREATE UNIQUE INDEX IF NOT EXISTS idx_attendances_lesson_enrollment ON Attendances(LessonId, EnrollmentId);",
          "CREATE INDEX IF NOT EXISTS idx_salaries_user_month_year ON Salaries(UserId, Month, Year);",
          "CREATE INDEX IF NOT EXISTS idx_tuitionfees_student_month_year ON TuitionFees(StudentId, Month, Year);",
          "CREATE INDEX IF NOT EXISTS idx_tuitionfees_feetype ON TuitionFees(FeeType);",
          "CREATE INDEX IF NOT EXISTS idx_tuitionfees_ispaid ON TuitionFees(IsPaid);"
        };
        foreach (var sql in statements)
        {
          using (var cmd = new SQLiteCommand(sql, conn))
          {
            cmd.ExecuteNonQuery();
          }
        }
        try
        {
          using var colCmd = new SQLiteCommand("ALTER TABLE Courses ADD COLUMN MonthlyFee DECIMAL(18,2) NOT NULL DEFAULT 0;", conn);
          colCmd.ExecuteNonQuery();
        }
        catch { }
      }
    }
    private static async Task SeedAsync(EduFlowDbContext db)
    {
      string hash = BCrypt.Net.BCrypt.HashPassword("123456");
      var admin = new User { Username = "admin", FullName = "Admin", Phone = "0901234567", Email = "admin@eduflow.vn", Role = UserRole.Admin, PasswordHash = hash, IsActive = true };
      db.Users.Add(admin);
      await db.SaveChangesAsync();
    }
  }
}