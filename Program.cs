using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using EcommerceApp.Data;
using EcommerceApp.Models;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
    });

var app = builder.Build();

// Apply migrations / create DB and seed
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    db.Database.EnsureCreated();
    // Seed admin user if not present
    if (!db.Users.Any(u => u.Role == "Admin"))
    {
        var admin = new User
        {
            UserName = "admin",
            Email = "admin@example.com",
            Role = "Admin",
            PasswordHash = HashPassword("Admin@123")
        };
        db.Users.Add(admin);
    }
    // Seed seller and buyer sample accounts
    if (!db.Users.Any(u => u.UserName == "seller1"))
    {
        db.Users.Add(new User {
            UserName = "seller1",
            Email = "seller1@example.com",
            Role = "Seller",
            PasswordHash = HashPassword("Seller@123")
        });
    }
    if (!db.Users.Any(u => u.UserName == "buyer1"))
    {
        db.Users.Add(new User {
            UserName = "buyer1",
            Email = "buyer1@example.com",
            Role = "Buyer",
            PasswordHash = HashPassword("Buyer@123")
        });
    }
    db.SaveChanges();

    // Ensure images folder exists under wwwroot
    var imagesDir = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "images");
    if (!Directory.Exists(imagesDir))
        Directory.CreateDirectory(imagesDir);

    // Seed sample products if none
    if (!db.Products.Any())
    {
        db.Products.Add(new Product { Name = "Sample T-Shirt", Description = "Comfortable cotton t-shirt", Price = 299.99M, ImagePath = "images/sample-tshirt.jpg", SellerId = db.Users.FirstOrDefault(u => u.UserName=="seller1")?.Id });
        db.Products.Add(new Product { Name = "Sample Mug", Description = "Ceramic coffee mug", Price = 149.50M, ImagePath = "images/sample-mug.jpg", SellerId = db.Users.FirstOrDefault(u => u.UserName=="seller1")?.Id });
        db.SaveChanges();
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

static string HashPassword(string password)
{
    using var sha = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(password);
    var hash = sha.ComputeHash(bytes);
    return Convert.ToHexString(hash);
}
