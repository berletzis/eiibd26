# Copilot Instructions

## Project Guidelines
- Project preference: When DataTable/GridData AJAX handlers fail in admin pages, apply one or more of: add an OnPostGridDataAsync shim when client uses POST, or switch client to GET; add [IgnoreAntiforgeryToken] and [Authorize(Roles = "Administrador")] to AJAX handlers; use .IgnoreQueryFilters() when restoring soft-deleted records; ensure image DB values are converted to absolute URLs prefixed with '/uploads/contenidos/...' and use lazy loading. In the frontend, use attributes in images: loading="lazy", decoding="async", fetchpriority="low", and specify dimensions (width/height). Also prefer WebOptimizer for bundling/minification and CSP entries for Google Tag Manager. Future recommendation: generate WebP/AVIF formats and srcset for cards/hero/grid, and implement preloading for the largest contentful paint (LCP) image.

- In ASP.NET Core Razor Pages with nullable reference types (.NET 8+), declare parameters of OnPost*Async handlers as string? and DateTime? (nullable). Non-nullable types implicitly activate [Required] in the model binder, causing a 400 error when the field is empty from the form.

- In the eiibd26 project, NEVER use `dotnet ef database update` in production. Schema changes are applied with direct SQL. The project uses net8.0, Razor Pages, EF Core 8, SQL Server, Hangfire, and ASP.NET Identity with roles Paciente/Medico/Administrador.

## Documentation Structure
- The source of truth for all project documentation for eiibd26 is always: D:\Users\berletzis\Source\Repos\eiibd\eiibd26\Documentation — never docs/ or any other path.