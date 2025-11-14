using eiibd26.Models;
using eiibd26.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.UI.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Registrar ApplicationDbContext con SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

// Registrar Identity con ApplicationUser y ApplicationRole personalizados
builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    // Aquí puedes ajustar opciones de password, lockout, etc.
    // options.Password.RequiredLength = 8;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Configure cookie paths so that challenges redirect to Identity login page
builder.Services.ConfigureApplicationCookie(options =>
{
    // Path to the Identity login page
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ReturnUrlParameter = "ReturnUrl";
    // Optional cookie settings
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Filtro de excepción para desarrollo
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// Servicio de EmailSender (SendGrid)
builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();

// Razor Pages
builder.Services.AddRazorPages();

// API Controllers
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// If a request results in a status code like 404, re-execute pipeline to show a friendly NotFound page.
// This will handle cases where endpoints exist but return 404. For APIs you may prefer default behavior;
// this approach will still execute the NotFound page — if you want JSON for API 404s, you can refine this later.
app.UseStatusCodePagesWithReExecute("/NotFound");

// Map redirects for common "login" routes to the Identity login page.
// This makes /login and similar direct to the canonical Identity login.
app.MapGet("/login", ctx =>
{
    ctx.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});
app.MapGet("/signin", ctx =>
{
    ctx.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});
app.MapGet("/account/login", ctx =>
{
    ctx.Response.Redirect("/Identity/Account/Login");
    return Task.CompletedTask;
});

// Aquí se mapean tus páginas Razor y tus controladores API.
app.MapRazorPages();
app.MapControllers(); // <<--- ESTA LÍNEA ASEGURA QUE FUNCIONAN LOS ENDPOINTS /api/...

// Catch-all for routes that didn't match any endpoint: show the NotFound page (friendly 404).
// MapFallbackToPage only applies when no other endpoint matches.
app.MapFallbackToPage("/NotFound");

app.Run();