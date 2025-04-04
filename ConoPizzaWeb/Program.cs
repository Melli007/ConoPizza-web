using ConoPizzaWeb.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR; 
using ConoPizzaWeb.Hubs;
using DinkToPdf.Contracts;
using DinkToPdf;
using OfficeOpenXml;


var builder = WebApplication.CreateBuilder(args);

ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

// Add DbContext.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")).EnableSensitiveDataLogging();
});

// Configure Identity to use the built‑in IdentityUser.
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Configure password policies as needed.
    options.Password.RequireDigit = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Register distributed cache service (required for session state)
builder.Services.AddDistributedMemoryCache();

// Add session support.
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Adjust timeout as needed
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true; // Set to true if the session cookie is required for your app to function
});

// Register DinkToPdf converter:
builder.Services.AddSingleton(typeof(IConverter), new SynchronizedConverter(new PdfTools()));

// Add support for controllers with views and Razor Pages.
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();

// 1️⃣ Add SignalR service
builder.Services.AddSignalR();

// Inside your Program.cs, add this to your service registration section
builder.Services.AddSingleton<ConoPizzaWeb.Services.SecureIdService>(
    new ConoPizzaWeb.Services.SecureIdService(
        builder.Configuration["SecureIds:Salt"], // Optionally get salt from configuration
        10 // Minimum hash length for secure IDs
    )
);

var app = builder.Build();

// Seed the admin user
await DataSeeder.SeedAdminUser(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}
app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Add session middleware (must be added after UseRouting and before UseEndpoints)
app.UseSession();

app.UseAuthentication(); // Enable authentication middleware.
app.UseAuthorization();

// 2️⃣ Map your OrdersHub (or any other Hub)
app.MapHub<OrdersHub>("/ordersHub");
app.MapHub<FeedbackHub>("/feedbackHub");

// Default route.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
