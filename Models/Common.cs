global using System;
global using System.Collections.Generic;
global using System.IO;
global using System.Linq;
global using System.Threading;
global using System.Threading.Tasks;
namespace System.Runtime.CompilerServices
{
  internal static class IsExternalInit { }
}
namespace EduFlow.Models.Common
{
  public abstract class BaseEntity
  {
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
  }
  public class BusinessException : Exception
  {
    public BusinessException(string message) : base(message) { }
    public BusinessException(string message, Exception inner) : base(message, inner) { }
  }
  public interface IVersioned
  {
    Guid RowVersion { get; set; }
  }
  public abstract class Person : BaseEntity
  {
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }
    public abstract string RoleName { get; }
    public virtual string DisplayName => $"{FullName} ({Phone})";
    public override string ToString() => $"{RoleName}: {DisplayName}";
  }
}