using System;
using System.Collections.Generic;
using System.Linq;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
namespace EduFlow.Services.Scheduling
{
  public static class ScheduleCalculator
  {
    public static List<Lesson> GenerateLessons(ClassRoom cls, int lessonCount)
    {
      if (cls.Days == WeekDays.None)
        throw new BusinessException("Phải Chọn Ít Nhất 1 Ngày Học Trong Tuần");
      if (cls.StartTime >= cls.EndTime)
        throw new BusinessException("Giờ Bắt Đầu Phải Trước Giờ Kết Thúc");
      if (lessonCount <= 0)
        throw new BusinessException("Số Buổi Phải Lớn Hơn 0");
      var lessons = new List<Lesson>();
      DateTime date = cls.StartDate.Date;
      while (lessons.Count < lessonCount)
      {
        if (cls.Days.HasFlag(date.DayOfWeek.ToWeekDay()))
        {
          lessons.Add(new Lesson
          {
            LessonNo = lessons.Count + 1,
            Date = date,
            StartTime = cls.StartTime,
            EndTime = cls.EndTime,
            PhysicalRoomId = cls.PhysicalRoomId,
            TeacherId = cls.TeacherId
          });
        }
        date = date.AddDays(1);
      }
      return lessons;
    }
    public static (Lesson New, Lesson Existing)? FindConflict(
      IEnumerable<Lesson> newLessons, IEnumerable<Lesson> existingLessons)
    {
      var existingByDate = existingLessons.ToLookup(s => s.Date.Date);
      foreach (var s in newLessons)
      {
        var hit = existingByDate[s.Date.Date].FirstOrDefault(e => e.OverlapsWith(s));
        if (hit != null) return (s, hit);
      }
      return null;
    }
  }
}