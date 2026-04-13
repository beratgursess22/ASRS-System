using ASRS.BLL.Services;
using ASRS.Core.Entities;
using ASRS.Core.Enums;
using ASRS.Core.Interfaces;
using ASRS.DAL.Context;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// MySQL bağlantısı
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 36))
    ));

// Identity tanımı
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireLowercase = false;
})
.AddEntityFrameworkStores<AppDbContext>()
.AddDefaultTokenProviders();

// Cookie ayarları
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";
});

// Servis kayıtları
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IWorkOrderService, WorkOrderService>();
builder.Services.AddScoped<IBomService, BomService>();
builder.Services.AddScoped<IMaterialService, MaterialService>();
builder.Services.AddScoped<IPurchaseRequestService, PurchaseRequestService>();
builder.Services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
builder.Services.AddScoped<ISupplierService, SupplierService>();
builder.Services.AddScoped<IQualityInspectionService, QualityInspectionService>();
builder.Services.AddScoped<IQualityDefectService, QualityDefectService>();
builder.Services.AddScoped<ICapaService, CapaService>();

//end

builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient("AsrsApi", client =>
{
    var baseUrl = builder.Configuration["AsrsApi:BaseUrl"] ?? "http://localhost:5217/";
    client.BaseAddress = new Uri(baseUrl);
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
    app.UseHttpsRedirection();
}

// .step/.stp dosyalarini static olarak yayinla
var contentTypeProvider = new FileExtensionContentTypeProvider();
contentTypeProvider.Mappings[".step"] = "application/step";
contentTypeProvider.Mappings[".stp"] = "application/step";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = contentTypeProvider
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

// // BOM seed — ürünlere örnek reçete ekler
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//     if (!db.BillOfMaterials.Any() && db.Products.Count() >= 5)
//     {
//         var products = db.Products.OrderBy(p => p.Id).ToList();

//         // Servo Motor 24V (products[1]) için reçete
//         // products[0] req=1  → Yeterli  (neredeyse her stokta karşılanır)
//         // products[3] req=9999 → Yetersiz
//         // products[4] req=9999 → Yetersiz
//         db.BillOfMaterials.AddRange(
//             new BillOfMaterial { ProductId = products[1].Id, ComponentProductId = products[0].Id, RequiredQuantity = 1,    Notes = "Motor muhafazası için profil" },
//             new BillOfMaterial { ProductId = products[1].Id, ComponentProductId = products[3].Id, RequiredQuantity = 9999, Notes = "Motor kimlik kartı — stok yetersiz!" },
//             new BillOfMaterial { ProductId = products[1].Id, ComponentProductId = products[4].Id, RequiredQuantity = 9999, Notes = "Sürücü step motor — stok yetersiz!" }
//         );

//         // Konveyör Bant (products[2]) için reçete
//         // products[0] req=9999 → Yetersiz
//         // products[1] req=1  → Yeterli
//         // products[4] req=1  → Yeterli
//         db.BillOfMaterials.AddRange(
//             new BillOfMaterial { ProductId = products[2].Id, ComponentProductId = products[0].Id, RequiredQuantity = 9999, Notes = "Konveyör taşıyıcı profil — stok yetersiz!" },
//             new BillOfMaterial { ProductId = products[2].Id, ComponentProductId = products[1].Id, RequiredQuantity = 1,    Notes = "Tahrik motoru" },
//             new BillOfMaterial { ProductId = products[2].Id, ComponentProductId = products[4].Id, RequiredQuantity = 1,    Notes = "Gergi step motoru" }
//         );

//         await db.SaveChangesAsync();
//     }
// }

// // Seed: Roller ve Admin kullanıcı
// using (var scope = app.Services.CreateScope())
// {
//     var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();
//     var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();

//     // Roller
//     string[] roles = ["Yönetici", "Depo", "Lojistik", "Üretim", "Kalite"];
//     foreach (var role in roles)
//         if (!await roleManager.RoleExistsAsync(role))
//             await roleManager.CreateAsync(new AppRole { Name = role, Description = role + " rolü" });

//     // Admin kullanıcı
//     if (await userManager.FindByEmailAsync("admin@asrs.com") == null)
//     {
//         var admin = new AppUser
//         {
//             UserName = "admin@asrs.com",
//             Email = "admin@asrs.com",
//             FirstName = "Admin",
//             LastName = "ASRS",
//             IsActive = true,
//             CreatedAt = DateTime.UtcNow,
//             EmailConfirmed = true
//         };
//         await userManager.CreateAsync(admin, "Admin123!");
//         await userManager.AddToRoleAsync(admin, "Yönetici");
//     }

//     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//     // Departman seed
//     if (!db.Departments.Any())
//     {
//         db.Departments.AddRange(
//             new Department { Name = "Üretim",   Description = "Üretim departmanı",          IsActive = true },
//             new Department { Name = "Depo",     Description = "Depo ve lojistik departmanı", IsActive = true },
//             new Department { Name = "Kalite",   Description = "Kalite kontrol departmanı",   IsActive = true },
//             new Department { Name = "Lojistik", Description = "Lojistik departmanı",         IsActive = true }
//         );
//         await db.SaveChangesAsync();
//     }

//     // Ürün seed
//     if (!db.Products.Any())
//     {
//         db.Products.AddRange(
//             new Product { Code = "PRD-001", Name = "Alüminyum Profil 40x40", Category = "Mekanik",    Unit = "Metre", StockQuantity = 150, MinStockLevel = 20 },
//             new Product { Code = "PRD-002", Name = "Servo Motor 24V",        Category = "Elektronik", Unit = "Adet",  StockQuantity = 30,  MinStockLevel = 5  },
//             new Product { Code = "PRD-003", Name = "Konveyör Bant",          Category = "Mekanik",    Unit = "Metre", StockQuantity = 60,  MinStockLevel = 10 },
//             new Product { Code = "PRD-004", Name = "RFID Kart",              Category = "Elektronik", Unit = "Adet",  StockQuantity = 200, MinStockLevel = 50 },
//             new Product { Code = "PRD-005", Name = "Step Motor NEMA17",      Category = "Elektronik", Unit = "Adet",  StockQuantity = 8,   MinStockLevel = 10 }
//         );
//         await db.SaveChangesAsync();
//     }

// Malzeme Seed - Test verileri
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
//     if (!db.Materials.Any())
//     {
//         db.Materials.AddRange(
//             new Material { Code = "MAT-001", Name = "M6 Vida", Unit = "Adet", StockQuantity = 500, MinStockLevel = 100, Description = "M6x20 İmbus Vida" },
//             new Material { Code = "MAT-002", Name = "T-Nut", Unit = "Adet", StockQuantity = 400, MinStockLevel = 80, Description = "M6 T-Nut" },
//             new Material { Code = "MAT-003", Name = "Rulman 608", Unit = "Adet", StockQuantity = 50, MinStockLevel = 20, Description = "608ZZ Rulman" },
//             new Material { Code = "MAT-004", Name = "Kablo 1mm²", Unit = "Metre", StockQuantity = 200, MinStockLevel = 50, Description = "Siyah Kablo" },
//             new Material { Code = "MAT-005", Name = "Somun M6", Unit = "Adet", StockQuantity = 600, MinStockLevel = 150, Description = "Altı köşe somun" }
//         );
//         await db.SaveChangesAsync();
//     }
// }

//     // İş emri seed
//     if (!db.WorkOrders.Any())
//     {
//         var adminUser = await userManager.FindByEmailAsync("admin@asrs.com");
//         var dept      = db.Departments.First();
//         var products  = db.Products.ToList();

//         db.WorkOrders.AddRange(
//             new WorkOrder
//             {
//                 OrderNumber       = "WO-20260311-001",
//                 Title             = "Alüminyum Profil Kesim ve Montaj",
//                 ProductId         = products[0].Id,
//                 Quantity          = 50,
//                 Priority          = WorkOrderPriority.High,
//                 Status            = WorkOrderStatus.InProgress,
//                 DepartmentId      = dept.Id,
//                 CreatedByUserId   = adminUser!.Id,
//                 PlannedStartDate  = new DateTime(2026, 3, 10),
//                 PlannedEndDate    = new DateTime(2026, 3, 20),
//                 Notes             = "Seri üretim için profil kesimi yapılacak.",
//                 CreatedAt         = new DateTime(2026, 3, 9)
//             },
//             new WorkOrder
//             {
//                 OrderNumber       = "WO-20260311-002",
//                 Title             = "Servo Motor Montaj Hattı",
//                 ProductId         = products[1].Id,
//                 Quantity          = 10,
//                 Priority          = WorkOrderPriority.Medium,
//                 Status            = WorkOrderStatus.Approved,
//                 DepartmentId      = dept.Id,
//                 CreatedByUserId   = adminUser!.Id,
//                 PlannedStartDate  = new DateTime(2026, 3, 15),
//                 PlannedEndDate    = new DateTime(2026, 3, 25),
//                 Notes             = "ASRS sistemi için motor montajı.",
//                 CreatedAt         = new DateTime(2026, 3, 10)
//             },
//             new WorkOrder
//             {
//                 OrderNumber       = "WO-20260311-003",
//                 Title             = "RFID Kart Stok Sayımı",
//                 ProductId         = products[3].Id,
//                 Quantity          = 100,
//                 Priority          = WorkOrderPriority.Low,
//                 Status            = WorkOrderStatus.Draft,
//                 DepartmentId      = dept.Id,
//                 CreatedByUserId   = adminUser!.Id,
//                 PlannedStartDate  = new DateTime(2026, 3, 18),
//                 PlannedEndDate    = new DateTime(2026, 3, 19),
//                 Notes             = "Depodaki RFID kartların sayımı ve etiketlenmesi.",
//                 CreatedAt         = new DateTime(2026, 3, 11)
//             },
//             new WorkOrder
//             {
//                 OrderNumber       = "WO-20260311-004",
//                 Title             = "Step Motor NEMA17 Sipariş Hazırlığı",
//                 ProductId         = products[4].Id,
//                 Quantity          = 20,
//                 Priority          = WorkOrderPriority.High,
//                 Status            = WorkOrderStatus.WaitingForMaterial,
//                 DepartmentId      = dept.Id,
//                 CreatedByUserId   = adminUser!.Id,
//                 PlannedStartDate  = new DateTime(2026, 3, 12),
//                 PlannedEndDate    = new DateTime(2026, 3, 22),
//                 Notes             = "Stok kritik seviyenin altında, malzeme bekleniyor.",
//                 CreatedAt         = new DateTime(2026, 3, 11)
//             },
//             new WorkOrder
//             {
//                 OrderNumber       = "WO-20260305-001",
//                 Title             = "Konveyör Bant Değişimi",
//                 ProductId         = products[2].Id,
//                 Quantity          = 15,
//                 Priority          = WorkOrderPriority.Medium,
//                 Status            = WorkOrderStatus.Completed,
//                 DepartmentId      = dept.Id,
//                 CreatedByUserId   = adminUser!.Id,
//                 PlannedStartDate  = new DateTime(2026, 3, 5),
//                 PlannedEndDate    = new DateTime(2026, 3, 8),
//                 Notes             = "Hat 2 konveyör bandı değiştirildi.",
//                 CreatedAt         = new DateTime(2026, 3, 4),
//                 CompletedAt       = new DateTime(2026, 3, 8)
//             }
//         );
//         await db.SaveChangesAsync();
//     }
// }


// Temel rol ve departman seed
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
//     var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<AppRole>>();

//     string[] roles = ["Yönetici", "Depo", "Lojistik", "Üretim", "Kalite", "Satın Alma"];
//     foreach (var role in roles)
//     {
//         if (!await roleManager.RoleExistsAsync(role))
//         {
//             await roleManager.CreateAsync(new AppRole
//             {
//                 Name = role,
//                 Description = $"{role} rolü"
//             });
//         }
//     }

//     var departmentsToEnsure = new[]
//     {
//         ("Üretim", "Üretim departmanı"),
//         ("Depo", "Depo departmanı"),
//         ("Kalite", "Kalite departmanı"),
//         ("Lojistik", "Lojistik departmanı"),
//         ("Satın Alma", "Satın alma departmanı")
//     };

//     foreach (var (name, description) in departmentsToEnsure)
//     {
//         var exists = await db.Departments.AnyAsync(d => d.Name == name);
//         if (!exists)
//         {
//             db.Departments.Add(new Department
//             {
//                 Name = name,
//                 Description = description,
//                 IsActive = true
//             });
//         }
//     }

//     await db.SaveChangesAsync();
// }

// Supplier seed (idempotent)
// using (var scope = app.Services.CreateScope())
// {
//     var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

//     var supplierSeeds = new[]
//     {
//         new Supplier
//         {
//             Code = "SUP-001",
//             Name = "Anadolu Metal Sanayi",
//             ContactPerson = "Mehmet Kara",
//             Email = "satin.alma@anadolumetal.com",
//             Phone = "+90 212 555 10 01",
//             Address = "Ikitelli OSB, Basaksehir, Istanbul",
//             IsActive = true
//         },
//         new Supplier
//         {
//             Code = "SUP-002",
//             Name = "Tekno Bilesen Dis Ticaret",
//             ContactPerson = "Elif Yilmaz",
//             Email = "teklif@teknobilesen.com",
//             Phone = "+90 216 555 10 02",
//             Address = "Dudullu OSB, Umraniye, Istanbul",
//             IsActive = true
//         },
//         new Supplier
//         {
//             Code = "SUP-003",
//             Name = "Rota Rulman ve Guc Aktarim",
//             ContactPerson = "Onur Demir",
//             Email = "satis@rotarulman.com",
//             Phone = "+90 224 555 10 03",
//             Address = "Nilufer Organize Sanayi, Bursa",
//             IsActive = true
//         },
//         new Supplier
//         {
//             Code = "SUP-004",
//             Name = "Volt Elektrik Malzemeleri",
//             ContactPerson = "Ayse Cetin",
//             Email = "kurumsal@voltelektrik.com",
//             Phone = "+90 312 555 10 04",
//             Address = "Ostim OSB, Yenimahalle, Ankara",
//             IsActive = true
//         },
//         new Supplier
//         {
//             Code = "SUP-005",
//             Name = "HidroLine Endustriyel",
//             ContactPerson = "Burak Aslan",
//             Email = "teklif@hidroline.com",
//             Phone = "+90 232 555 10 05",
//             Address = "AOSB, Cigli, Izmir",
//             IsActive = true
//         },
//         new Supplier
//         {
//             Code = "SUP-006",
//             Name = "EuroMotion Components GmbH",
//             ContactPerson = "Anna Fischer",
//             Email = "procurement@euromotion.de",
//             Phone = "+49 30 555 10 06",
//             Address = "Berlin, Germany",
//             IsActive = true
//         },
//         new Supplier
//         {
//             Code = "SUP-007",
//             Name = "Eski Partner Mekanik",
//             ContactPerson = "Kerem Aydin",
//             Email = "info@eskipartner.com",
//             Phone = "+90 850 555 10 07",
//             Address = "Gebze, Kocaeli",
//             IsActive = false
//         }
//     };

//     var existingCodes = await db.Suppliers
//         .Select(x => x.Code)
//         .ToListAsync();

//     var existingSet = new HashSet<string>(existingCodes, StringComparer.OrdinalIgnoreCase);

//     foreach (var seed in supplierSeeds)
//     {
//         if (existingSet.Contains(seed.Code))
//             continue;

//         seed.CreatedAt = DateTime.UtcNow;
//         db.Suppliers.Add(seed);
//     }

//     await db.SaveChangesAsync();
// }

app.Run();
