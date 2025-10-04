using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PortalAcademico.Data;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// ✅ CONFIGURACIÓN PARA PRODUCCIÓN Y DESARROLLO
if (builder.Environment.IsProduction())
{
    // PostgreSQL para producción (Render)
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(connectionString));
}
else
{
    // SQLite para desarrollo local
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(connectionString));
}

builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = false)
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddControllersWithViews();

// ✅ CONFIGURAR REDIS PARA CACHE DISTRIBUIDO
var redisConnectionString = builder.Configuration.GetValue<string>("Redis:ConnectionString") 
    ?? "localhost:6379";

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnectionString;
    options.InstanceName = builder.Configuration.GetValue<string>("Redis:InstanceName") ?? "PortalAcademico:";
});

// ✅ CONFIGURAR SESIONES CON REDIS
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name = ".PortalAcademico.Session";
});

// ✅ REGISTRAR SERVICIO DE CACHE
builder.Services.AddScoped<PortalAcademico.Services.ICacheService, PortalAcademico.Services.CacheService>();

var app = builder.Build();

// ✅ SEED DATA INICIAL (Solo si la BD está vacía)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        await SeedData.Initialize(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error al ejecutar seed data");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    // ✅ HSTS para producción
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// ✅ IMPORTANTE: UseSession debe ir antes de UseAuthorization
app.UseSession();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();