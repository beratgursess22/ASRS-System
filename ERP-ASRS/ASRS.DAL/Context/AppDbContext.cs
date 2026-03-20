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
	public DbSet<Material> Materials { get; set; }
	public DbSet<PurchaseRequest> PurchaseRequests { get; set; }
	public DbSet<PurchaseRequestItem> PurchaseRequestItems { get; set; }
	public DbSet<PurchaseOrder> PurchaseOrders { get; set; }
	public DbSet<PurchaseOrderItem> PurchaseOrderItems { get; set; }
	public DbSet<Supplier> Suppliers { get; set; }
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
		modelBuilder.Entity<BillOfMaterial>()
			.HasOne(b => b.Material)
			.WithMany()
			.HasForeignKey(b => b.MaterialId)
			.OnDelete(DeleteBehavior.Restrict);
		modelBuilder.Entity<PurchaseRequest>()
			.HasOne(pr => pr.WorkOrder)
			.WithMany()
			.HasForeignKey(pr => pr.WorkOrderId)
			.OnDelete(DeleteBehavior.Cascade);
		modelBuilder.Entity<PurchaseRequest>()
			.HasOne(pr => pr.RequestedByUser)
			.WithMany()
			.HasForeignKey(pr => pr.RequestedByUserId)
			.OnDelete(DeleteBehavior.Restrict);
		modelBuilder.Entity<PurchaseRequestItem>()
			.HasOne(i => i.PurchaseRequest)
			.WithMany(pr => pr.Items)
			.HasForeignKey(i => i.PurchaseRequestId)
			.OnDelete(DeleteBehavior.Cascade);
		modelBuilder.Entity<PurchaseRequestItem>()
			.HasOne(i => i.Product)
			.WithMany()
			.HasForeignKey(i => i.ProductId)
			.OnDelete(DeleteBehavior.Restrict);
		modelBuilder.Entity<PurchaseRequestItem>()
			.HasOne(i => i.Material)
			.WithMany()
			.HasForeignKey(i => i.MaterialId)
			.OnDelete(DeleteBehavior.Restrict);
		modelBuilder.Entity<PurchaseOrder>()
			.HasOne(po => po.PurchaseRequest)
			.WithMany()
			.HasForeignKey(po => po.PurchaseRequestId)
			.OnDelete(DeleteBehavior.Restrict);
		modelBuilder.Entity<PurchaseOrder>()
			.HasOne(po => po.CreatedByUser)
			.WithMany()
			.HasForeignKey(po => po.CreatedByUserId)
			.OnDelete(DeleteBehavior.Restrict);
		modelBuilder.Entity<PurchaseOrderItem>()
			.HasOne(i => i.PurchaseOrder)
			.WithMany(po => po.Items)
			.HasForeignKey(i => i.PurchaseOrderId)
			.OnDelete(DeleteBehavior.Cascade);
		modelBuilder.Entity<PurchaseOrderItem>()
			.HasOne(i => i.Product)
			.WithMany()
			.HasForeignKey(i => i.ProductId)
			.OnDelete(DeleteBehavior.Restrict);
		modelBuilder.Entity<PurchaseOrderItem>()
			.HasOne(i => i.Material)
			.WithMany()
			.HasForeignKey(i => i.MaterialId)
			.OnDelete(DeleteBehavior.Restrict);
		modelBuilder.Entity<PurchaseOrder>()
			.HasOne(po => po.Supplier)
			.WithMany()
			.HasForeignKey(po => po.SupplierId)
			.OnDelete(DeleteBehavior.SetNull);
	}
}
