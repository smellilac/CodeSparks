using CodeSparks.Data;
using CodeSparks.Data.Models;
using CodeSparks.Data.Seed;
using CodeSparks.Services.Repositories;
using CodeSparks.Temp;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
var hostingProvider = Environment.GetEnvironmentVariable("HOSTING_PROVIDER");

bool isRender = hostingProvider?.ToLower() == "render.com";
#if DEBUG
isRender = true;
#endif

var connectionString = builder.Configuration.GetConnectionString("PostgresConnection");

// fallback to DefaultConnection
if (connectionString == null)
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<AppDbContext>(options =>
    //options.UseSqlServer(connectionString));
    options.UseNpgsql(connectionString));
//builder.Services.AddDbContext<DataProtectionKeyContext>(options =>
//            options.UseNpgsql(connectionString));
//builder.Services.AddDataProtection()
//    .PersistKeysToDbContext<DataProtectionKeyContext>()
//    .SetApplicationName("iqtify.com");
//builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
//    .AddCookie(options =>
//    {
//        options.Cookie.HttpOnly = true;
//        options.ExpireTimeSpan = TimeSpan.FromDays(30);
//        options.LoginPath = "/Identity/Account/Login";
//        options.LogoutPath = "/Identity/Account/Logout";
//        options.SlidingExpiration = true;
//    });

builder.Services.AddIdentity<AppUser, IdentityRole<Guid>>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole<Guid>>()
    .AddUserManager<UserManager<AppUser>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.AddHealthChecks();
builder.Services.AddCoreAdmin("Admin");
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddTransient<IEmailSender, InMemoryEmailSender>(); // Replace with your implementation
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IBadgeRepository, BadgeRepository>();

var app = builder.Build();
app.Logger.LogInformation("Application starting up");

using (var scope = app.Services.CreateScope())
{
    app.Logger.LogInformation("Seeding initial data.");
    var services = scope.ServiceProvider;
    var seeder = new DefaultDataSeeder(services);
    await seeder.SeedAsync();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

if (isRender)
{
    app.Logger.LogInformation("Deployed on render.com, no need in https");
}
else
{
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();

    app.UseHttpsRedirection();
}

// TODO: move later to dev env
app.UseDeveloperExceptionPage();

app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}");
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();
app.MapHealthChecks("/health");

app.Run();
