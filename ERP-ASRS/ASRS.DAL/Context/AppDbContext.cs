using System.Security.Cryptography.X509Certificates;
using ASRS.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ASRS.DAL.Context;

public class AppDbContext : IdentityDbContext<AppUser, AppRole, string> // "IdentityDbContext" sınıfından türetilmiş bir DbContext sınıfı, kullanıcı ve rol yönetimi için gerekli tabloları içerir
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Department> Departments { get; set; }
	public DbSet<Product> Products { get; set; } 
	public DbSet<WorkOrder> WorkOrders { get; set; }
	public DbSet<BillOfMaterial> BillOfMaterials { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<AppUser>().ToTable("Users");
        modelBuilder.Entity<AppRole>().ToTable("Roles");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserRole<string>>().ToTable("UserRoles");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserClaim<string>>().ToTable("UserClaims");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserLogin<string>>().ToTable("UserLogins");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityRoleClaim<string>>().ToTable("RoleClaims");
        modelBuilder.Entity<Microsoft.AspNetCore.Identity.IdentityUserToken<string>>().ToTable("UserTokens");

        modelBuilder.Entity<AppUser>()
            .HasOne(u => u.Department)
            .WithMany(d => d.Users)
            .HasForeignKey(u => u.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // BillOfMaterial: iki ayrı Product FK — EF Core'un otomatik çözmesi için explicit tanım
        modelBuilder.Entity<BillOfMaterial>()
            .HasOne(b => b.Product)
            .WithMany()
            .HasForeignKey(b => b.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<BillOfMaterial>()
            .HasOne(b => b.ComponentProduct)
            .WithMany()
            .HasForeignKey(b => b.ComponentProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
