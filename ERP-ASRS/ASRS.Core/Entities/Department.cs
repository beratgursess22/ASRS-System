using System.ComponentModel;
using System.Data.SqlTypes;
using System.Security;
using System.Security.Cryptography.X509Certificates;

namespace ASRS.Core.Entities;

public class Department
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty; // null olmaması için string.Empty atandı
	public string? Description { get; set; }
	public bool IsActive {get; set; } = true;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
	public ICollection<AppUser> Users { get; set; } = new List<AppUser>();
	// Department (1)  --------  (N) AppUser // Bir Department birden fazla AppUser içerebilir, ancak her AppUser sadece bir Department'a ait olabilir.
}
