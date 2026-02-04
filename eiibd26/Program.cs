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
// Register Razor Pages and add route convention so SEO URL /Preguntas/{slug} maps to the Detalles page
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        // Map the SEO-friendly route /Preguntas/{slug} to the page at /Preguntas/Detalles
        options.Conventions.AddPageRoute("/Preguntas/Detalles", "/Preguntas/{slug}");
    });
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

    // Ignorar si ya es una petición interna a páginas Razor
    if (path.StartsWith("/Contenidos/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Preguntas/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/Home/", StringComparison.OrdinalIgnoreCase))
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

    // Ignorar rutas conocidas (framework, assets, APIs)
    var knownPrefixes = new[]
    {
        "identity/", "api/", "account/", "_framework/", "css/", "js/",
        "img/", "uploads/", "lib/", "swagger/", "favicon.ico", "robots.txt",
        "sitemap.xml", "error", "notfound", ".well-known/"
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

    using var scope = context.RequestServices.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // ===== CASO 1: /c/{contentSlug} → Contenido sin categoría =====
    if (segments.Length == 2 && segments[0].Equals("c", StringComparison.OrdinalIgnoreCase))
    {
        var contentSlug = segments[1];

        logger.LogInformation("=== CASO 1: Contenido sin categoría /c/{ContentSlug} ===", contentSlug);

        var exists = await db.Contenidos
            .AsNoTracking()
            .AnyAsync(c => c.ContenidoTituloSlug == contentSlug && !c.Eliminado);

        if (exists)
        {
            logger.LogInformation("✅ Contenido existe, reescribiendo a /Contenidos/Detalle");
            context.Request.Path = "/Contenidos/Detalle";
            context.Request.QueryString = new QueryString($"?slug={Uri.EscapeDataString(contentSlug)}");
        }
        else
        {
            logger.LogWarning("❌ Contenido /c/{ContentSlug} no existe", contentSlug);
        }
    }

    // ===== CASO 2: /{categorySlug}/{contentSlug} → Contenido con categoría =====
    else if (segments.Length == 2)
    {
        var categorySlug = segments[0];
        var contentSlug = segments[1];

        logger.LogInformation("=== CASO 2: Contenido con categoría /{CategorySlug}/{ContentSlug} ===", categorySlug, contentSlug);

        var category = await db.ContenidosCategorias
            .AsNoTracking()
            .Where(c => c.CategoriaSlug == categorySlug && !c.Borrado)
            .Select(c => new { c.Sequence, c.Nombre })
            .FirstOrDefaultAsync();

        if (category != null)
        {
            logger.LogInformation("✅ Categoría '{Nombre}' encontrada (ID={Sequence})", category.Nombre, category.Sequence);

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
                logger.LogInformation("✅ Contenido pertenece a la categoría, reescribiendo URL");
                context.Request.Path = "/Contenidos/Detalle";
                context.Request.QueryString = new QueryString($"?categorySlug={Uri.EscapeDataString(categorySlug)}&slug={Uri.EscapeDataString(contentSlug)}");
            }
            else
            {
                logger.LogWarning("⚠️ Contenido NO pertenece a esta categoría, buscando categoría real...");

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
                              (rel, cat) => new { cat.CategoriaSlug, cat.CategoriaPadre, cat.Nombre })
                        .OrderBy(x => x.CategoriaPadre.HasValue ? 0 : 1)
                        .FirstOrDefaultAsync();

                    if (realCat != null && !string.IsNullOrWhiteSpace(realCat.CategoriaSlug))
                    {
                        logger.LogInformation("🔄 Redirigiendo 301 a categoría real: /{RealCat}/{ContentSlug}", realCat.CategoriaSlug, contentSlug);
                        context.Response.Redirect($"/{realCat.CategoriaSlug}/{contentSlug}", permanent: true);
                        return;
                    }
                    else
                    {
                        logger.LogInformation("🔄 Contenido sin categoría, redirigiendo a /c/{ContentSlug}", contentSlug);
                        context.Response.Redirect($"/c/{contentSlug}", permanent: true);
                        return;
                    }
                }
                else
                {
                    logger.LogWarning("❌ Contenido '{ContentSlug}' no existe", contentSlug);
                }
            }
        }
        else
        {
            logger.LogWarning("❌ Categoría '{CategorySlug}' no existe", categorySlug);
        }
    }

    // ===== CASO 3: /{categorySlug} → Listado de categoría =====
    else if (segments.Length == 1)
    {
        var categorySlug = segments[0];

        logger.LogInformation("=== CASO 3: Listado de categoría /{CategorySlug} ===", categorySlug);

        var exists = await db.ContenidosCategorias
            .AsNoTracking()
            .AnyAsync(c => c.CategoriaSlug == categorySlug && !c.Borrado);

        if (exists)
        {
            logger.LogInformation("✅ Categoría existe, reescribiendo a /Contenidos/categoria/{CategorySlug}", categorySlug);

            // CORREGIDO: Usar la ruta correcta de la página Razor
            context.Request.Path = $"/Contenidos/categoria/{categorySlug}";
            context.Request.QueryString = QueryString.Empty;
        }
        else
        {
            logger.LogWarning("❌ Categoría '{CategorySlug}' no existe", categorySlug);
        }
    }
    // ===== CASO 4: /u/{slug} → Perfil público de usuario =====
    else if (segments.Length == 2 && segments[0].Equals("u", StringComparison.OrdinalIgnoreCase))
    {
        var userSlug = segments[1];

        logger.LogInformation("=== CASO 4: Perfil público /u/{UserSlug} ===", userSlug);

        var exists = await db.Perfil
            .AsNoTracking()
            .AnyAsync(p => p.slug == userSlug);

        if (exists)
        {
            logger.LogInformation("✅ Perfil público existe, procesando /u/{UserSlug}", userSlug);
            // NO reescribir: ASP.NET Routing manejará /u/{slug} directamente
        }
        else
        {
            logger.LogWarning("❌ Perfil público /u/{UserSlug} no existe", userSlug);
        }
    }

    await next();
});

// UseRouting DESPUÉS del middleware de reescritura
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseStatusCodePagesWithReExecute("/NotFound");

// Atajos de login
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

app.MapControllers();
app.MapRazorPages();

app.Run();