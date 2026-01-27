using eiibd26.Data;
using eiibd26.Helpers;
using eiibd26.Models;
using eiibd26.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ReturnUrlParameter = "ReturnUrl";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

builder.Services.AddDatabaseDeveloperPageExceptionFilter();
builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();
builder.Services.AddTransient<ISmsSender, TwilioSmsSender>();
builder.Services.AddRazorPages();
builder.Services.AddControllers();

var app = builder.Build();

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

// ===== MIDDLEWARE SEO (ANTES DE UseRouting) =====
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? "";

    // Ignorar si ya es una petición interna
    if (path.StartsWith("/Contenidos/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Preguntas/", StringComparison.OrdinalIgnoreCase))
    {
        await next();
        return;
    }

    path = path.TrimStart('/');
    if (string.IsNullOrWhiteSpace(path))
    {
        await next();
        return;
    }

    var pathLower = path.ToLowerInvariant();

    // Ignorar rutas conocidas
    var knownPrefixes = new[]
    {
        "identity/", "api/", "account/", "_framework/", "css/", "js/",
        "img/", "uploads/", "lib/", "swagger/", "favicon.ico", "robots.txt",
        "sitemap.xml", "error", "notfound"
    };

    if (knownPrefixes.Any(p => pathLower.StartsWith(p)))
    {
        await next();
        return;
    }

    var segments = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

    if (segments.Length == 0)
    {
        await next();
        return;
    }

    // ===== CASO 1: /c/{contentSlug} =====
    if (segments.Length == 2 && segments[0].Equals("c", StringComparison.OrdinalIgnoreCase))
    {
        var contentSlug = segments[1];

        using var scope = context.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var exists = await db.Contenidos
            .AsNoTracking()
            .AnyAsync(c => c.ContenidoTituloSlug == contentSlug && !c.Eliminado);

        if (exists)
        {
            context.Request.Path = "/Contenidos/Detalle";
            context.Request.QueryString = new QueryString($"?slug={Uri.EscapeDataString(contentSlug)}");
            // NO llamar await next() aquí, dejar que el routing lo maneje
        }
    }

    // ===== CASO 2: /{categorySlug}/{contentSlug} =====
    else if (segments.Length == 2)
    {
        var categorySlug = segments[0];
        var contentSlug = segments[1];

        using var scope = context.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("=== SEO MIDDLEWARE ===");
        logger.LogInformation("categorySlug: '{CategorySlug}'", categorySlug);
        logger.LogInformation("contentSlug: '{ContentSlug}'", contentSlug);

        var category = await db.ContenidosCategorias
            .AsNoTracking()
            .Where(c => c.CategoriaSlug == categorySlug && !c.Borrado)
            .Select(c => new { c.Sequence, c.Nombre })
            .FirstOrDefaultAsync();

        if (category != null)
        {
            logger.LogInformation("✅ Categoría encontrada: {Nombre}", category.Nombre);

            var contentId = await db.Contenidos
                .AsNoTracking()
                .Where(c => c.ContenidoTituloSlug == contentSlug && !c.Eliminado)
                .Join(db.ContenidosCategoriasRelacion,
                      content => content.Id,
                      rel => rel.IdContenido,
                      (content, rel) => new { content.Id, rel.IdCategoria, rel.Borrado })
                .Where(x => !x.Borrado && x.IdCategoria == category.Sequence)
                .Select(x => x.Id)
                .FirstOrDefaultAsync();

            if (contentId != 0)
            {
                logger.LogInformation("✅ Reescribiendo URL internamente");
                context.Request.Path = "/Contenidos/Detalle";
                context.Request.QueryString = new QueryString($"?categorySlug={Uri.EscapeDataString(categorySlug)}&slug={Uri.EscapeDataString(contentSlug)}");
                // NO llamar await next() aquí
            }
            else
            {
                // Buscar categoría real y redirigir 301
                var content = await db.Contenidos
                    .AsNoTracking()
                    .Where(c => c.ContenidoTituloSlug == contentSlug && !c.Eliminado)
                    .Select(c => c.Id)
                    .FirstOrDefaultAsync();

                if (content != 0)
                {
                    var realCat = await db.ContenidosCategoriasRelacion
                        .AsNoTracking()
                        .Where(r => r.IdContenido == content && !r.Borrado && r.IdCategoria != null)
                        .Join(db.ContenidosCategorias,
                              rel => rel.IdCategoria,
                              cat => cat.Sequence,
                              (rel, cat) => new { cat.CategoriaSlug, cat.CategoriaPadre })
                        .OrderBy(x => x.CategoriaPadre.HasValue ? 0 : 1)
                        .FirstOrDefaultAsync();

                    if (realCat != null && !string.IsNullOrWhiteSpace(realCat.CategoriaSlug))
                    {
                        logger.LogInformation("🔄 Redirigiendo 301 a categoría real");
                        context.Response.Redirect($"/{realCat.CategoriaSlug}/{contentSlug}", permanent: true);
                        return;
                    }

                    context.Response.Redirect($"/c/{contentSlug}", permanent: true);
                    return;
                }
            }
        }
    }

    // ===== CASO 3: /{categorySlug} =====
    else if (segments.Length == 1)
    {
        var categorySlug = segments[0];

        using var scope = context.RequestServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var exists = await db.ContenidosCategorias
            .AsNoTracking()
            .AnyAsync(c => c.CategoriaSlug == categorySlug && !c.Borrado);

        if (exists)
        {
            context.Request.Path = "/Contenidos/porCategoria";
            context.Request.QueryString = new QueryString($"?categorySegment={Uri.EscapeDataString(categorySlug)}");
        }
    }

    await next();
});

// UseRouting DESPUÉS del middleware de reescritura
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute("/NotFound");

app.MapGet("/login", ctx => { ctx.Response.Redirect("/Identity/Account/Login"); return Task.CompletedTask; });
app.MapGet("/signin", ctx => { ctx.Response.Redirect("/Identity/Account/Login"); return Task.CompletedTask; });

app.MapControllers();
app.MapRazorPages();

app.Run();