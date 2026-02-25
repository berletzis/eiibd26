// Program.cs (mejorado para proteger /Identity y permitir solo páginas públicas)
using eiibd26.Data;
using eiibd26.Helpers;
using eiibd26.Models;
using eiibd26.Services;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Diagnostics;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Configuración de DbContext con resiliencia de errores transitorios
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 5,
            maxRetryDelay: TimeSpan.FromSeconds(30),
            errorNumbersToAdd: null);

        // ===== PERFORMANCE: Optimizaciones adicionales =====
        sqlOptions.CommandTimeout(30); // Timeout explícito de 30 segundos
        sqlOptions.MaxBatchSize(100); // Optimizar batch inserts/updates
    }));

builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
{
    // Sign-in options
    options.SignIn.RequireConfirmedAccount = false;
    options.User.RequireUniqueEmail = true;

    // Password policy (fortalecida)
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    options.Password.RequiredUniqueChars = 4;

    // Lockout settings (protección contra fuerza bruta)
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
})
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

// Cookie configuration (seguridad mejorada)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ReturnUrlParameter = "ReturnUrl";
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;

    // Security: HTTPS-only cookies en producción, protección CSRF
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax; // Changed from Strict to Lax for AJAX compatibility
});

// Authorization: políticas
builder.Services.AddAuthorization(options =>
{
    // Política para administradores (ajusta el nombre del rol si tu DB usa otro)
    options.AddPolicy("AdminOnly", policy =>
    {
        policy.RequireRole("Administrador");
    });

    // no definimos una FallbackPolicy global aquí porque se protegerá el area Identity por convención
});

// Register Razor Pages and add route convention + authorization conventions
builder.Services.AddRazorPages()
    .AddRazorPagesOptions(options =>
    {
        // Map SEO-friendly route /Preguntas/{slug} -> /Preguntas/Detalles
        options.Conventions.AddPageRoute("/Preguntas/Detalles", "/Preguntas/{slug}");

        // PROTECCIÓN: autorizar TODO el área Identity por defecto
        options.Conventions.AuthorizeAreaFolder("Identity", "/");

        // Exponer explícitamente las páginas públicas de Identity (permitir anónimo)
        options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Login");
        options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/Register");
        options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ForgotPassword");
        options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ResetPassword");
        options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/ConfirmEmail");
        options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/AccessDenied");
        // Si tienes otras páginas públicas (ConfirmEmailChange, ExternalLoginCallback...) añádelas también.

        // Protección por roles (ejemplo): /Identity/Admin -> Administradores solamente
        options.Conventions.AuthorizeAreaFolder("Identity", "/Admin", "AdminOnly");

        // Si quieres proteger sólo /Account/Manage puedes usar:
        // options.Conventions.AuthorizeAreaFolder("Identity", "/Account/Manage");
    });

builder.Services.AddControllers();
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

// ===== PERFORMANCE: Response Caching =====
builder.Services.AddResponseCaching();
builder.Services.AddMemoryCache();

// ===== PERFORMANCE: WebOptimizer (Bundling & Minification) =====
builder.Services.AddWebOptimizer(pipeline =>
{
    // Bundle CSS: Combina todos los archivos CSS en uno
    pipeline.AddCssBundle("/css/bundle.min.css",
        "css/site.css",
        "css/miSalud.css",
        "css/account.css",
        "css/site-responsive.css")
        .UseContentRoot();

    // Minifica todos los archivos JS individuales
    pipeline.MinifyJsFiles("js/**/*.js");

    // Minifica archivos CSS individuales (útil para páginas específicas)
    pipeline.MinifyCssFiles("css/**/*.css");
});

// ===== PERFORMANCE: Response Compression (Gzip/Brotli) =====
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProvider>();
    options.Providers.Add<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProvider>();
    options.MimeTypes = Microsoft.AspNetCore.ResponseCompression.ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "image/svg+xml", "application/json", "application/javascript" });
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.BrotliCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

builder.Services.Configure<Microsoft.AspNetCore.ResponseCompression.GzipCompressionProviderOptions>(options =>
{
    options.Level = System.IO.Compression.CompressionLevel.Fastest;
});

// HSTS mejorado para producción
builder.Services.AddHsts(options =>
{
    options.MaxAge = TimeSpan.FromDays(365);
    options.IncludeSubDomains = true;
    options.Preload = true;
});

builder.Services.AddTransient<IEmailSender, SendGridEmailSender>();
builder.Services.AddTransient<ISmsSender, TwilioSmsSender>();

var app = builder.Build();

// Diagnostic middleware: on local requests or when query `showException=1` is present,
// return exception details in the response to aid debugging without enabling global Development env.
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception ex)
    {
        var logger = context.RequestServices.GetService(typeof(ILogger<Program>)) as ILogger;
        var requestId = Activity.Current?.Id ?? context.TraceIdentifier;
        try { logger?.LogError(ex, "Unhandled exception (RequestId={RequestId})", requestId); } catch { }

        bool isLocal = false;
        try
        {
            var ip = context.Connection.RemoteIpAddress;
            if (ip != null && (IPAddress.IsLoopback(ip) || ip.ToString() == "::1")) isLocal = true;
        }
        catch { }

        if (isLocal || (context.Request.Query.TryGetValue("showException", out var v) && v == "1") || context.Request.Headers["X-Debug"] == "1")
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/plain; charset=utf-8";
            await context.Response.WriteAsync($"RequestId: {requestId}\n\n{ex}");
            return;
        }

        // rethrow to let existing exception handler middleware process it in production
        throw;
    }
});

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

// ===== SECURITY HEADERS MIDDLEWARE =====
app.Use(async (context, next) =>
{
    // Protección contra clickjacking
    context.Response.Headers.Add("X-Frame-Options", "DENY");

    // Prevenir MIME-sniffing
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");

    // Protección XSS (legacy browsers)
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");

    // Política de referrer
    context.Response.Headers.Add("Referrer-Policy", "strict-origin-when-cross-origin");

    // Content Security Policy (ajustada para tu aplicación)
    var cspBuilder = new StringBuilder();
    cspBuilder.Append("default-src 'self'; ");

    // Scripts: App, CDNs, Google Maps, Cloudflare y TinyMCE
    cspBuilder.Append("script-src 'self' 'unsafe-inline' 'unsafe-eval' blob: " +
                      "https://cdn.jsdelivr.net " +
                      "https://unpkg.com " +
                      "https://maps.googleapis.com " +
                      "https://cdn.datatables.net " +
                      "https://code.jquery.com " +
                      "https://cdnjs.cloudflare.com " +
                      "https://static.cloudflareinsights.com " +
                      "https://cdn.tiny.cloud " +
                      "https://www.googletagmanager.com " +
                      "https://www.google-analytics.com " +
                      "https://googleads.g.doubleclick.net " +
                      "https://www.google.com " +
                      "https://api.cloudflare.com; ");

    // Explicit element-level/script-src-elem for newer CSP checks (e.g. Tag Assistant)
    // Include common CDNs (jsdelivr, code.jquery.com), Chart.js and allow inline scripts where necessary
    cspBuilder.Append("script-src-elem 'self' 'unsafe-inline' blob: " +
                      "https://cdn.jsdelivr.net " +
                      "https://cdn.jsdelivr.net/npm " +
                      "https://code.jquery.com " +
                      "https://unpkg.com " +
                      "https://maps.googleapis.com " +
                      "https://www.googletagmanager.com " +
                      "https://www.google-analytics.com " +
                      "https://googleads.g.doubleclick.net " +
                      "https://www.google.com " +
                      "https://www.googleadservices.com " +
                      "https://stats.g.doubleclick.net " +
                      "https://static.cloudflareinsights.com " +
                      "https://cloudflareinsights.com " +
                      "https://cdn.datatables.net " +
                      "https://api.cloudflare.com " +
                      "https://cdnjs.cloudflare.com " +
                      "https://cdn.tiny.cloud; ");

    // Allow inline handlers (script-src-attr) and include CDNs + Google domains
    cspBuilder.Append("script-src-attr 'self' 'unsafe-inline' blob: " +
                      "https://cdn.jsdelivr.net " +
                      "https://cdn.jsdelivr.net/npm " +
                      "https://code.jquery.com " +
                      "https://unpkg.com " +
                      "https://maps.googleapis.com " +
                      "https://www.googletagmanager.com " +
                      "https://www.google-analytics.com " +
                      "https://googleads.g.doubleclick.net " +
                      "https://www.google.com " +
                      "https://static.cloudflareinsights.com " +
                      "https://cloudflareinsights.com " +
                      "https://cdn.datatables.net " +
                      "https://cdnjs.cloudflare.com " +
                      "https://cdn.tiny.cloud; ");

    // Estilos: App, CDNs, Google Fonts y TinyMCE
    cspBuilder.Append("style-src 'self' 'unsafe-inline' " +
                      "https://cdn.jsdelivr.net " +
                      "https://fonts.googleapis.com " +
                      "https://cdn.datatables.net " +
                      "https://cdnjs.cloudflare.com " +
                      "https://cdn.tiny.cloud; ");

    // Fuentes: Google Fonts y Bootstrap Icons en CDN
    cspBuilder.Append("font-src 'self' " +
                      "https://fonts.gstatic.com " +
                      "https://cdn.jsdelivr.net " +
                      "data:; ");

    // Imágenes: permitir blob: para imágenes dinámicas (avatares, etc.)
    cspBuilder.Append("img-src 'self' data: https: blob:; ");

    // Conexiones: permitir localhost en desarrollo para Hot Reload y Browser Link
    if (app.Environment.IsDevelopment())
        {
        // In development allow local hosts and common analytics endpoints used during testing
        cspBuilder.Append("connect-src 'self' " +
                          "http://localhost:* " +
                          "ws://localhost:* " +
                          "wss://localhost:* " +
                          "https://cdn.jsdelivr.net " +
                          "https://unpkg.com " +
                          "https://maps.googleapis.com " +
                          "https://static.cloudflareinsights.com " +
                          "https://cloudflareinsights.com " +
                          "https://cdn.tiny.cloud " +
                          // Allow Google analytics/endpoints the gtag script may call
                          "https://analytics.google.com " +
                          "https://www.google-analytics.com " +
                          "https://googleads.g.doubleclick.net " +
                          "https://www.google.com " +
                          "https://api.cloudflare.com; ");
    }
    else
    {
        // Allow Google Analytics/Measurement Protocol & Ads endpoints used by gtag and analytics
        cspBuilder.Append("connect-src 'self' " +
                          "https://eiibd.com " +
                          "https://www.eiibd.com " +
                          "https://cdn.jsdelivr.net " +
                          "https://unpkg.com " +
                          "https://maps.googleapis.com " +
                          "https://static.cloudflareinsights.com " +
                          "https://cloudflareinsights.com " +
                          "https://cdn.tiny.cloud " +
                          "https://analytics.google.com " +
                          "https://www.google-analytics.com " +
                          "https://googleads.g.doubleclick.net " +
                          "https://www.google.com " +
                          "https://api.cloudflare.com; ");
    }

    // Frames: permitir Google Maps y Google Tag Manager (necesario para algunos contenedores/preview)
    cspBuilder.Append("frame-src 'self' https://maps.googleapis.com https://www.googletagmanager.com https://tagmanager.google.com;");

    context.Response.Headers.Add("Content-Security-Policy", cspBuilder.ToString());

    // Permissions Policy (permitir geolocalización para el mapa)
    context.Response.Headers.Add("Permissions-Policy",
        "geolocation=(self), microphone=(), camera=()");

    await next();
});

app.UseHttpsRedirection();

// ===== PERFORMANCE: Enable Response Compression =====
// Enable response compression except when running in Development to help
// diagnose potential double-compression / decoding issues under IIS Express.
if (!app.Environment.IsDevelopment())
{
    app.UseResponseCompression();
}

// ===== PERFORMANCE: Enable Response Caching =====
app.UseResponseCaching();

// ===== PERFORMANCE: WebOptimizer (debe ir ANTES de UseStaticFiles) =====
app.UseWebOptimizer();

// ===== PERFORMANCE: Static Files con caching headers =====
app.UseStaticFiles(new StaticFileOptions
{
    OnPrepareResponse = ctx =>
    {
        // Conservative caching: keep long-term cache for static assets but use short cache for
        // user-uploaded avatars so updates propagate quickly in production.
        var reqPath = ctx.Context.Request.Path.Value ?? string.Empty;
        if (reqPath.StartsWith("/uploads/avatars/", StringComparison.OrdinalIgnoreCase))
        {
            // Short cache for avatars (1 minute) to reduce stale images while still allowing
            // some client-side caching.
            ctx.Context.Response.Headers["Cache-Control"] = "public, max-age=60, must-revalidate";
            ctx.Context.Response.Headers["Expires"] = DateTime.UtcNow.AddMinutes(1).ToString("R");
        }
        else
        {
            // Cache estático: CSS, JS, imágenes por 1 año (usa versionado para invalidar)
            const int durationInSeconds = 31536000; // 1 año
            ctx.Context.Response.Headers["Cache-Control"] = $"public, max-age={durationInSeconds}";
            ctx.Context.Response.Headers["Expires"] = DateTime.UtcNow.AddYears(1).ToString("R");
        }
    }
});

// ===== MIDDLEWARE SEO (antes de UseRouting) - mantengo tu lógica =====
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

        logger.LogDebug("=== CASO 1: Contenido sin categoría /c/{ContentSlug} ===", contentSlug);

        var exists = await db.Contenidos
            .AsNoTracking()
            .AnyAsync(c => c.ContenidoTituloSlug == contentSlug && !c.Eliminado);

        if (exists)
        {
            context.Request.Path = "/Contenidos/Detalle";
            context.Request.QueryString = new QueryString($"?slug={Uri.EscapeDataString(contentSlug)}");
        }
    }
    // ===== CASO 2: /{categorySlug}/{contentSlug} → Contenido con categoría =====
    else if (segments.Length == 2)
    {
        var categorySlug = segments[0];
        var contentSlug = segments[1];

        var category = await db.ContenidosCategorias
            .AsNoTracking()
            .Where(c => c.CategoriaSlug == categorySlug && !c.Borrado)
            .Select(c => new { c.Sequence, c.Nombre })
            .FirstOrDefaultAsync();

        if (category != null)
        {
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
                context.Request.Path = "/Contenidos/Detalle";
                context.Request.QueryString = new QueryString($"?categorySlug={Uri.EscapeDataString(categorySlug)}&slug={Uri.EscapeDataString(contentSlug)}");
            }
            else
            {
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
                        context.Response.Redirect($"/{realCat.CategoriaSlug}/{contentSlug}", permanent: true);
                        return;
                    }
                    else
                    {
                        context.Response.Redirect($"/c/{contentSlug}", permanent: true);
                        return;
                    }
                }
            }
        }
    }
    // ===== CASO 3: /{categorySlug} → Listado de categoría =====
    else if (segments.Length == 1)
    {
        var categorySlug = segments[0];

        var exists = await db.ContenidosCategorias
            .AsNoTracking()
            .AnyAsync(c => c.CategoriaSlug == categorySlug && !c.Borrado);

        if (exists)
        {
            // Reescribir a la página de categoría (ajusta si tienes otra ruta)
            context.Request.Path = $"/Contenidos/categoria/{categorySlug}";
            context.Request.QueryString = QueryString.Empty;
        }
    }
    // ===== CASO 4: /u/{slug} → Perfil público de usuario =====
    else if (segments.Length == 2 && segments[0].Equals("u", StringComparison.OrdinalIgnoreCase))
    {
        var userSlug = segments[1];

        var exists = await db.Perfil
            .AsNoTracking()
            .AnyAsync(p => p.slug == userSlug);

        if (exists)
        {
            // dejar que routing lo maneje
        }
    }

    await next();
});

// UseRouting DESPUÉS del middleware de reescritura
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

// Middleware: si la cookie indica un usuario pero ese usuario ya no existe en la DB,
// forzar sign-out y redirigir al login para evitar errores como "Unable to load user with ID ...".
app.Use(async (context, next) =>
{
    try
    {
        if (context.User?.Identity != null && context.User.Identity.IsAuthenticated)
        {
            var userIdClaim = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!string.IsNullOrEmpty(userIdClaim))
            {
                // resolver DB por scope
                using var scope = context.RequestServices.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                if (!Guid.TryParse(userIdClaim, out var userGuid))
                {
                    // claim inválido: forzar signout
                    await context.SignOutAsync();
                    context.Response.Redirect("/Identity/Account/Login");
                    return;
                }

                var exists = await db.Users.AsNoTracking().AnyAsync(u => u.Id == userGuid);
                if (!exists)
                {
                    // user was removed from DB, sign out and redirect to login
                    await context.SignOutAsync();
                    context.Response.Redirect("/Identity/Account/Login");
                    return;
                }
            }
        }
    }
    catch (Exception)
    {
        // ignore and continue; we don't want middleware errors breaking app startup
    }
    await next();
});

// Opcional: re-ejecutar NotFound en /NotFound
app.UseStatusCodePagesWithReExecute("/NotFound");

// Shortcuts to login
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
// Pega este fragmento en Program.cs (antes de app.MapControllers()/app.MapRazorPages())
app.MapGet("/robots.txt", async ctx =>
{
    ctx.Response.ContentType = "text/plain; charset=utf-8";
    var host = $"{ctx.Request.Scheme}://{ctx.Request.Host}";
    var sb = new StringBuilder();
    sb.AppendLine("User-agent: *");
    sb.AppendLine("Disallow:");
    sb.AppendLine(); // espacios opcionales
    sb.AppendLine($"Sitemap: {host}/sitemap.xml");
    await ctx.Response.WriteAsync(sb.ToString());
});

app.MapControllers();
app.MapRazorPages();


app.Run();