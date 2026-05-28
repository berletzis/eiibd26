// Program.cs (mejorado para proteger /Identity y permitir solo páginas públicas)
using eiibd26.Data;
using Hangfire;
using eiibd26.Helpers;
using eiibd26.Models;
using eiibd26.Services;
using Microsoft.AspNetCore.Authentication;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Diagnostics;
using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

// Global startup exception handler — writes the full exception to stdout/stderr
// so it appears in the IIS stdout log (.\logs\stdout_*.log) before the 500.30 page.
try
{

    var builder = WebApplication.CreateBuilder(args);

    builder.Configuration.AddJsonFile("appsettings.local.json", optional: true, reloadOnChange: true);

    // Validate connection string early — empty string is as bad as null in production.
    var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
    if (string.IsNullOrWhiteSpace(connectionString))
    {
        Console.Error.WriteLine("[STARTUP] FATAL: ConnectionStrings:DefaultConnection is missing or empty.");
        Console.Error.WriteLine("[STARTUP] Set it in appsettings.Production.json on the server. See SECRETS.md.");
        throw new InvalidOperationException(
            "Connection string 'DefaultConnection' not found or empty. " +
            "Set ConnectionStrings:DefaultConnection in appsettings.Production.json on the server.");
    }

    // Configuración de DbContext con resiliencia de errores transitorios
    builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlServer(connectionString, sqlOptions =>
        {
            sqlOptions.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(30),
                errorNumbersToAdd: null);

            // ===== PERFORMANCE: Optimizaciones adicionales =====
            sqlOptions.CommandTimeout(120); // Remote SQL Server needs longer timeout without proper indexes
            sqlOptions.MaxBatchSize(100); // Optimizar batch inserts/updates
        }));

    // ⭐ NUEVO: Localización en español para mensajes de Identity
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

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
        .AddDefaultTokenProviders()
        .AddErrorDescriber<SpanishIdentityErrorDescriber>();

    // ⭐ NUEVO: Persistir Data Protection Keys (para tokens de reseteo de contraseña)
    var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
    var dataProtectionReady = false;
    try
    {
        Directory.CreateDirectory(dataProtectionPath);
        dataProtectionReady = true;
    }
    catch (Exception dpEx)
    {
        Console.Error.WriteLine($"[STARTUP WARNING] Cannot create DataProtectionKeys directory at '{dataProtectionPath}': {dpEx.Message}");
        Console.Error.WriteLine("[STARTUP WARNING] Data Protection will use in-memory keys (tokens lost on restart). " +
                                "Grant write permissions to the app pool identity on the site folder to fix this.");
    }

    var dpBuilder = builder.Services.AddDataProtection()
        .SetApplicationName("eiibd26")
        .SetDefaultKeyLifetime(TimeSpan.FromDays(90));

    if (dataProtectionReady)
        dpBuilder.PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath));

    // Configurar tiempo de vida de tokens de reseteo de contraseña
    builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
    {
        options.TokenLifespan = TimeSpan.FromDays(3); // 24 horas
    });

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

    // SEC-001/002/003: Rate limiting para endpoints de catálogos públicos (autocomplete).
    // Previene scraping masivo. 30 requests por IP por ventana de 60 segundos.
    builder.Services.AddRateLimiter(options =>
    {
        options.AddFixedWindowLimiter("catalogos-autocomplete", limiterOptions =>
        {
            limiterOptions.Window = TimeSpan.FromSeconds(60);
            limiterOptions.PermitLimit = 30;
            limiterOptions.QueueLimit = 0;
        });
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.OnRejected = async (context, ct) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
            await context.HttpContext.Response.WriteAsJsonAsync(
                new { ok = false, error = "Demasiadas solicitudes. Intente nuevamente en un momento." }, ct);
        };
    });

    // Register Razor Pages and add route convention + authorization conventions
    var razorBuilder = builder.Services.AddRazorPages();
    if (builder.Environment.IsDevelopment())
        razorBuilder.AddRazorRuntimeCompilation();
    razorBuilder.AddRazorPagesOptions(options =>
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
            options.Conventions.AllowAnonymousToAreaPage("Identity", "/Account/RegisterM");
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
            "css/usuario-condiciones-crm.css",
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

    builder.Services.AddTransient<SendGridEmailSender>();
    builder.Services.AddTransient<IEmailSender>(sp => sp.GetRequiredService<SendGridEmailSender>());
    builder.Services.AddTransient<ISmsSender, TwilioSmsSender>();

    // PWA Push Notifications
    builder.Services.AddScoped<eiibd26.Services.PushNotificationService>();
    builder.Services.AddSingleton(TimeProvider.System);
    builder.Services.AddSingleton<eiibd26.Services.IPushMoodTokenService, eiibd26.Services.PushMoodTokenService>();

    // Search Suggestions (sin IA)
    builder.Services.AddScoped<eiibd26.Services.SearchSuggestionService>();

    // SEC-010/011: Validación de ownership para datos clínicos del paciente
    builder.Services.AddScoped<eiibd26.Services.ClinicalOwnershipValidator>();

    // ⭐ NUEVO: Background Service para procesar notificaciones programadas
    builder.Services.AddHostedService<eiibd26.Services.ScheduledNotificationWorker>();

    // ===== AI ANSWER SERVICES =====
    // Configure AI Answer settings
    builder.Services.Configure<eiibd26.Configuration.AiAnswerConfiguration>(
        builder.Configuration.GetSection("AiAnswer"));

    // Register AI services
    builder.Services.AddSingleton<eiibd26.Services.AI.IAiAnswerService, eiibd26.Services.AI.AiAnswerService>();
    builder.Services.AddSingleton<eiibd26.Services.AI.IAiPromptBuilder, eiibd26.Services.AI.AiPromptBuilder>();
    builder.Services.AddSingleton<eiibd26.Services.AI.IAiSafetyService, eiibd26.Services.AI.AiSafetyService>();
    builder.Services.AddScoped<eiibd26.Services.AI.ISimilarQuestionDetector, eiibd26.Services.AI.SimilarQuestionDetector>();
    // ⭐ NUEVO: Servicio especializado para generar descripciones de Síntomas y Tratamientos
    builder.Services.AddScoped<eiibd26.Services.AI.ISintomasTratamientosAiService, eiibd26.Services.AI.SintomasTratamientosAiService>();

    // ⭐ GLOSSARY MODULE: Servicios para navegación médica desacoplada
    builder.Services.AddScoped<eiibd26.Services.Glossary.Adapters.IMedicalDataAdapter, eiibd26.Services.Glossary.Adapters.MedicalDataAdapter>();
    builder.Services.AddScoped<eiibd26.Services.Glossary.IGlossaryService, eiibd26.Services.Glossary.GlossaryService>();
    builder.Services.AddScoped<eiibd26.Services.Community.ICommunityExperienceService, eiibd26.Services.Community.CommunityExperienceService>();

    // ── Analytics: Estadísticas de Salud ───────────────────────────────────────
    builder.Services.AddScoped<eiibd26.Services.Analytics.IHealthStatsService, eiibd26.Services.Analytics.HealthStatsService>();
    builder.Services.AddScoped<eiibd26.Services.Analytics.IHealthInsightService, eiibd26.Services.Analytics.HealthInsightService>();

    // ── Export: Resumen Médico PDF ──────────────────────────────────────────────
    QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;
    builder.Services.AddScoped<eiibd26.Services.Export.IMedicalSummaryService, eiibd26.Services.Export.MedicalSummaryService>();
    builder.Services.AddScoped<eiibd26.Services.Export.Pdf.IPdfGeneratorService, eiibd26.Services.Export.Pdf.PdfGeneratorService>();
    builder.Services.AddScoped<eiibd26.Services.Tracking.ITrackingSintomaService, eiibd26.Services.Tracking.TrackingSintomaService>();

    // Directorio comunitario de médicos EII
    builder.Services.AddScoped<eiibd26.Services.Directorio.IMedicoDirectorioService, eiibd26.Services.Directorio.MedicoDirectorioService>();
    builder.Services.AddScoped<eiibd26.Services.Medico.IMedicoBadgeService, eiibd26.Services.Medico.MedicoBadgeService>();
    builder.Services.AddScoped<eiibd26.Services.Validacion.IValidacionContenidoService, eiibd26.Services.Validacion.ValidacionContenidoService>();

    // Register HttpClient for Anthropic API
    builder.Services.AddHttpClient("AnthropicClient", (serviceProvider, client) =>
    {
        var config = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<eiibd26.Configuration.AiAnswerConfiguration>>().Value;
        var logger = serviceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("🔧 [HTTP CLIENT] Configurando AnthropicClient...");

        if (!string.IsNullOrWhiteSpace(config.ApiBaseUrl))
        {
            // Asegurar que BaseAddress termina con /
            var baseUrl = config.ApiBaseUrl.TrimEnd('/') + "/";
            client.BaseAddress = new Uri(baseUrl);
            logger.LogInformation("🔧 [HTTP CLIENT] BaseAddress configurado: {BaseUrl}", baseUrl);
        }
        else
        {
            logger.LogWarning("⚠️ [HTTP CLIENT] ApiBaseUrl NO configurado");
        }

        if (!string.IsNullOrWhiteSpace(config.AnthropicApiKey))
        {
            client.DefaultRequestHeaders.Add("x-api-key", config.AnthropicApiKey);
            logger.LogInformation("🔧 [HTTP CLIENT] API Key configurado (masked): {ApiKeyMasked}", eiibd26.Security.SecretsValidator.Mask(config.AnthropicApiKey));
        }
        else
        {
            logger.LogWarning("⚠️ [HTTP CLIENT] AnthropicApiKey NO configurado");
        }

        if (!string.IsNullOrWhiteSpace(config.ApiVersion))
        {
            client.DefaultRequestHeaders.Add("anthropic-version", config.ApiVersion);
            logger.LogInformation("🔧 [HTTP CLIENT] API Version configurado: {ApiVersion}", config.ApiVersion);
        }
        else
        {
            logger.LogWarning("⚠️ [HTTP CLIENT] ApiVersion NO configurado");
        }

        var timeout = config.TimeoutSeconds > 0 ? config.TimeoutSeconds : 30;
        client.Timeout = TimeSpan.FromSeconds(timeout);
        logger.LogInformation("🔧 [HTTP CLIENT] Timeout configurado: {Timeout}s", timeout);
    });

    builder.Services.AddScoped<eiibd26.Jobs.AiAnswerJob>();

    var hangfireConn = builder.Configuration.GetConnectionString("DefaultConnection");
    builder.Services.AddHangfire(cfg => cfg
        .SetDataCompatibilityLevel(CompatibilityLevel.Version_180)
        .UseSimpleAssemblyNameTypeSerializer()
        .UseRecommendedSerializerSettings()
        .UseSqlServerStorage(hangfireConn));
    builder.Services.AddHangfireServer(opt => opt.WorkerCount = 2);

    // Log AI service initialization
    var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("Startup");
    var aiConfig = builder.Configuration.GetSection("AiAnswer").Get<eiibd26.Configuration.AiAnswerConfiguration>();
    if (aiConfig != null)
    {
        logger.LogInformation("✅ AI Answer Services configured. Enabled: {Enabled}", aiConfig.Enabled);
        if (aiConfig.Enabled)
        {
            if (!string.IsNullOrWhiteSpace(aiConfig.AnthropicApiKey) && aiConfig.AnthropicApiKey != "ANTHROPIC_API_KEY_AQUI")
                logger.LogInformation("✅ Anthropic API Key configured");
            else
                logger.LogWarning("⚠️ Anthropic API Key NOT configured or using placeholder");

            if (aiConfig.SystemUserId != Guid.Empty)
                logger.LogInformation("✅ System User ID configured: {SystemUserId}", aiConfig.SystemUserId);
            else
                logger.LogWarning("⚠️ System User ID NOT configured (using empty GUID)");
        }
    }
    // ===== END AI ANSWER SERVICES =====

    var app = builder.Build();

    // SEC-015: Advertir si el ambiente no es Production — stack traces de API quedan expuestos en Development.
    if (!app.Environment.IsProduction())
    {
        var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
        startupLogger.LogWarning(
            "[SEC-015] ASPNETCORE_ENVIRONMENT = '{Env}'. Los endpoints de API retornan stack traces en ambientes no productivos. " +
            "Asegurarse que la variable ASPNETCORE_ENVIRONMENT=Production esté configurada en el servidor.",
            app.Environment.EnvironmentName);
    }

    // ===== SECRETS VALIDATION =====
    // Fails the app in Production if any critical secret is missing.
    // Logs a warning in Development to allow incremental onboarding.
    eiibd26.Security.SecretsValidator.ValidateOrThrow(
        app.Services.GetRequiredService<IConfiguration>(),
        app.Environment,
        app.Services.GetRequiredService<ILogger<Program>>());

    // Diagnostic middleware: logs unhandled exceptions with a RequestId for tracing.
    // Stack traces are never written to the HTTP response.
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

    // ⭐ NUEVO: Configurar localización en español
    var supportedCultures = new[] { "es-MX", "es-ES", "es" };
    var localizationOptions = new RequestLocalizationOptions
    {
        DefaultRequestCulture = new Microsoft.AspNetCore.Localization.RequestCulture("es-MX"),
        SupportedCultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList(),
        SupportedUICultures = supportedCultures.Select(c => new System.Globalization.CultureInfo(c)).ToList()
    };
    app.UseRequestLocalization(localizationOptions);

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

            // Case-insensitive slug lookup for category
            var normalizedCategorySlug = categorySlug.ToLowerInvariant();
            var category = await db.ContenidosCategorias
                .AsNoTracking()
                .Where(c => c.CategoriaSlug.ToLower() == normalizedCategorySlug && !c.Borrado)
                .Select(c => new { c.Sequence, c.Nombre })
                .FirstOrDefaultAsync();

            if (category != null)
            {
                // ✅ PRIMERO: Verificar si el segundo segmento es una categoría hija
                var normalizedContentSlug = contentSlug.ToLowerInvariant();
                var childCategory = await db.ContenidosCategorias
                    .AsNoTracking()
                    .Where(c => c.CategoriaSlug.ToLower() == normalizedContentSlug && !c.Borrado && c.CategoriaPadre == category.Sequence)
                    .FirstOrDefaultAsync();

                if (childCategory != null)
                {
                    // Es una categoría hija, no un contenido - redirigir a la categoría
                    context.Response.Redirect($"/{contentSlug}", permanent: true);
                    return;
                }

                // SEGUNDO: Buscar contenido con esa categoría específica
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
                    // TERCERO: Buscar contenido sin importar categoría
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

            // Case-insensitive slug lookup
            var normalizedSlug = categorySlug.ToLowerInvariant();
            var exists = await db.ContenidosCategorias
                .AsNoTracking()
                .AnyAsync(c => c.CategoriaSlug.ToLower() == normalizedSlug && !c.Borrado);

            if (exists)
            {
                // ✅ NO reescribir - dejar que Razor Pages maneje la ruta /{categorySegment}
                // La página porCategoria.cshtml ya está configurada con @page "/{categorySegment?}"
                // Simplemente continuar y Razor Pages capturará la solicitud
                await next();
                return;
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
    app.UseRateLimiter(); // SEC-001/002/003: rate limiting para catálogos
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
    app.MapGet("/registro", async context =>
    {
        context.Response.Redirect("/Identity/Account/Register");
    });


    app.MapGet("/animo", async context =>
    {
        context.Response.Redirect("/Identity/Usuario/Dashboard");
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

    app.UseHangfireDashboard("/hangfire", new Hangfire.DashboardOptions
    {
        Authorization = new[] { new eiibd26.Helpers.HangfireAdminAuthFilter() }
    });

    // Redirigir médicos a su dashboard cuando aterrizan en "/"
    app.Use(async (context, next) =>
    {
        if (context.User.Identity?.IsAuthenticated == true
            && context.User.IsInRole("Medico")
            && context.Request.Path == "/")
        {
            context.Response.Redirect("/Identity/Medico/Dashboard");
            return;
        }
        await next();
    });

    app.MapControllers();
    app.MapRazorPages();

    // Seed roles
    // SEC-012: El rol operativo del sistema es "Administrador" (usado en [Authorize(Roles="Administrador")],
    // HangfireAdminAuthFilter, y políticas). "Admin" se mantiene por compatibilidad con datos existentes en DB.
    using (var scope = app.Services.CreateScope())
    {
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var role in new[] { "Paciente", "Medico", "Admin", "Administrador" })
        {
            if (!await roleManager.RoleExistsAsync(role))
                await roleManager.CreateAsync(new ApplicationRole { Name = role });
        }
    }

    app.Run();

} // end try
catch (Exception ex)
{
    // Write the full exception to stdout so it appears in the IIS stdout log.
    // This is the only reliable way to diagnose HTTP 500.30 on IIS/ASP.NET Core Module.
    Console.Error.WriteLine("==========================================================");
    Console.Error.WriteLine("[STARTUP FATAL] Application failed to start.");
    Console.Error.WriteLine($"[STARTUP FATAL] {ex.GetType().FullName}: {ex.Message}");
    Console.Error.WriteLine("[STARTUP FATAL] Stack trace:");
    Console.Error.WriteLine(ex.ToString());
    Console.Error.WriteLine("==========================================================");
    throw; // re-throw so the process exits with a non-zero code (IIS sees the failure)
}