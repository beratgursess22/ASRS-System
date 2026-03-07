namespace ASRS.Core.Entities;

using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Identity;
using Microsoft.VisualBasic;

public class AppRole : IdentityRole // içinde ıd ve name  değişkenleri var 
{
    public string? Description { get; set; }
	public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
