using EduFlow.Data;
using EduFlow.Models.Common;
using EduFlow.Models.Entities;
using EduFlow.Models.Enums;
using EduFlow.Services.Common;
using System.Data.Entity;
using System.Data.Entity.Infrastructure;
namespace EduFlow.Services;
public interface ICatalogService
{
  Task<List<Room>> GetRoomsAsync(bool activeOnly = true);
  Task<Room> SaveRoomAsync(Room room);
  Task DeleteRoomAsync(int roomId);
}
public class CatalogService : BaseService, ICatalogService
{
  public CatalogService(IEduFlowDbContextFactory factory, UserSession session) : base(factory, session) { }
  public async Task<List<Room>> GetRoomsAsync(bool activeOnly = true)
  {
    using var db = CreateContext();
    IQueryable<Room> q = db.Rooms.AsNoTracking();
    if (activeOnly) q = q.Where(r => r.IsActive);
    return await q.OrderBy(r => r.Name).ToListAsync();
  }
  public async Task<Room> SaveRoomAsync(Room input)
  {
    Session.Require(Permission.ManageCatalog);
    if (string.IsNullOrWhiteSpace(input.Name)) throw new BusinessException("Tên Phòng Không Được Để Trống");
    if (input.Capacity <= 0) throw new BusinessException("Sức Chứa Phải Lớn Hơn 0");
    using var db = CreateContext();
    string name = input.Name.Trim();
    if (await db.Rooms.AnyAsync(r => r.Name == name && r.Id != input.Id))
      throw new BusinessException($"Phòng {name} Đã Tồn Tại");
    var room = input.Id == 0 ? db.Rooms.Add(new Room())
      : await db.Rooms.FindAsync(input.Id) ?? throw new BusinessException("Không Tìm Thấy Phòng");
    if (input.Id != 0)
    {
      if (!input.IsActive && await db.Classes.AnyAsync(c => c.PhysicalRoomId == input.Id && (c.Status == ClassStatus.Open || c.Status == ClassStatus.InProgress)))
        throw new BusinessException("Phòng Học Đang Có Lớp Hoạt Động, Không Thể Ngừng Sử Dụng");
      int maxRequired = await db.Classes
        .Where(c => c.PhysicalRoomId == input.Id && (c.Status == ClassStatus.Open || c.Status == ClassStatus.InProgress))
        .Select(c => (int?)c.MaxStudents)
        .MaxAsync() ?? 0;
      if (input.Capacity < maxRequired)
        throw new BusinessException($"Phòng Đang Có Lớp Sĩ Số Tối Đa {maxRequired} Học Viên, Không Thể Giảm Sức Chứa Xuống {input.Capacity}");
    }
    room.Name = name;
    room.Capacity = input.Capacity;
    room.IsActive = input.IsActive;
    await db.SaveChangesAsync();
    Audit(db, input.Id == 0 ? "Room.Create" : "Room.Update", "Room", room.Id, room.Name);
    await db.SaveChangesAsync();
    return room;
  }
  public async Task DeleteRoomAsync(int roomId)
  {
    Session.Require(Permission.ManageCatalog);
    using var db = CreateContext();
    var room = await db.Rooms.FindAsync(roomId)
      ?? throw new BusinessException("Không Tìm Thấy Phòng Học");
    if (await db.Classes.AnyAsync(c => c.PhysicalRoomId == roomId) || await db.Lessons.AnyAsync(l => l.PhysicalRoomId == roomId))
      throw new BusinessException("Phòng Học Đã Có Lớp Hoặc Lịch Học, Không Thể Xóa · Hãy Bỏ Chọn Đang Sử Dụng");
    db.Rooms.Remove(room);
    Audit(db, "Room.Delete", "Room", room.Id, room.Name);
    await db.SaveChangesAsync();
  }
}