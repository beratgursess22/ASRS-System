namespace ASRS.Core.Entities;

using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics.Contracts;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Microsoft.AspNetCore.Identity;

public class AppUser : IdentityUser // içinde ıd , username , password hash gibi değişkenler var
{
    public string FirstName { get; set; } = string.Empty; // null olmaması için string.Empty atandı
	public string LastName { get; set; } = string.Empty; // null olmaması için string.Empty atandı
	public string FullName => $"{FirstName} {LastName}".Trim();
	public int? DepartmentId { get; set; } // ilişki tablolar için  nul olabilir atanmamış olabilir 
	public Department? Department { get; set; } // EF Core tanıması için 
	public bool IsActive { get; set; } = true;
	public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

