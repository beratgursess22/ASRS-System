namespace ASRS.DAL.Context;

using ASRS.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using System.Reflection.Metadata;
using System.Reflection.Emit;

public class AppDbContext : IdentityDbContext<Appuser, AppRole, string>
{
	public AppContext(DbContextOptions<AppContext> options) : base(options) { }

	public DbSet<Department> Departments { get; set; }
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		builder.Entity<AppUser>().ToTable("Users");
		builder.Entity<AppRole>().ToTable("Roles");
		builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
		builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
		builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
		builder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
		builder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");

		builder.Entity<AppUser>()
			.HasOne(u => u.Department)
			.WithMany(d => d.Users)
			.HasForeignKey(u => u.DepartmentId)
			.OnDelete(DeleteBehavior.SetNull);
	}
}
