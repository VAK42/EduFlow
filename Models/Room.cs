using EduFlow.Models.Common;
namespace EduFlow.Models.Entities
{
  public class Room : BaseEntity
  {
    public string Name { get; set; } = string.Empty;
    public int Capacity { get; set; }
    public bool IsActive { get; set; } = true;
    public override string ToString() => $"{Name} · {Capacity} Chỗ";
  }
}