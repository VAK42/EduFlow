using System;
using EduFlow.Models.Common;
namespace EduFlow.Models.Entities
{
  public class AuditLog
  {
    public long Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    public int? UserId { get; set; }
    public User? User { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Entity { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string? Detail { get; set; }
  }
}