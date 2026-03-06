# 🔧 SOLUCIÓN: "El token es inválido" al Resetear Contraseña

## 🐛 Problemas Identificados

### 1. **Doble Codificación del Token**
El token se codifica en `ForgotPassword` pero se decodifica mal en `ResetPassword`.

### 2. **Data Protection Keys No Persistentes**
Cuando la app se reinicia, los tokens antiguos se invalidan.

### 3. **Token Expirado**
Por defecto, los tokens expiran en 1 día.

---

## ✅ SOLUCIÓN 1: Corregir Decodificación del Token

### Archivo: `ResetPassword.cshtml.cs`

**Problema en línea 85:**
```csharp
// ❌ INCORRECTO - Decodifica cuando NO debe
Input = new InputModel
{
    Code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code))
};
```

**Solución:**
```csharp
// ✅ CORRECTO - El code ya viene codificado correctamente
Input = new InputModel
{
    Code = code  // No decodificar aquí
};
```

**Y en OnPostAsync (línea 105):**
```csharp
// ❌ INCORRECTO - Usa Input.Code directamente
var result = await _userManager.ResetPasswordAsync(user, Input.Code, Input.Password);
```

**Solución:**
```csharp
// ✅ CORRECTO - Decodificar SOLO una vez antes de usar
var code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
var result = await _userManager.ResetPasswordAsync(user, code, Input.Password);
```

---

## ✅ SOLUCIÓN 2: Persistir Data Protection Keys

### Problema:
Cuando la app se reinicia, las claves de encriptación se regeneran y los tokens antiguos se invalidan.

### Solución: Persistir en Disco

**En `Program.cs`, agregar DESPUÉS de la línea donde se configura Identity:**

```csharp
// ⭐ NUEVO: Persistir Data Protection Keys
builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(
        builder.Environment.ContentRootPath, "DataProtectionKeys")))
    .SetApplicationName("eiibd26");
```

**Crear carpeta:**
```powershell
New-Item -ItemType Directory -Path "eiibd26\DataProtectionKeys" -Force
```

**Agregar a `.gitignore`:**
```
DataProtectionKeys/
```

---

## ✅ SOLUCIÓN 3: Extender Tiempo de Expiración del Token

### En `Program.cs`, después de `builder.Services.AddIdentity`:

```csharp
// ⭐ NUEVO: Configurar tiempo de expiración de tokens
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromHours(24); // 24 horas (default)
    // O más tiempo si es necesario:
    // options.TokenLifespan = TimeSpan.FromDays(3);
});
```

---

## 📝 Código Completo Corregido

### ResetPassword.cshtml.cs (CORREGIDO)

```csharp
using System;
using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;
using eiibd26.Models;

namespace eiibd26.Areas.Identity.Pages.Account
{
    public class ResetPasswordModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<ResetPasswordModel> _logger;

        public ResetPasswordModel(
            UserManager<ApplicationUser> userManager,
            ILogger<ResetPasswordModel> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        [BindProperty]
        public InputModel Input { get; set; }

        public class InputModel
        {
            [Required(ErrorMessage = "El email es requerido")]
            [EmailAddress(ErrorMessage = "Email inválido")]
            public string Email { get; set; }

            [Required(ErrorMessage = "La contraseña es requerida")]
            [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} y máximo {1} caracteres.", MinimumLength = 6)]
            [DataType(DataType.Password)]
            [Display(Name = "Nueva contraseña")]
            public string Password { get; set; }

            [DataType(DataType.Password)]
            [Display(Name = "Confirmar contraseña")]
            [Compare("Password", ErrorMessage = "La contraseña y la confirmación no coinciden.")]
            public string ConfirmPassword { get; set; }

            [Required]
            public string Code { get; set; }
        }

        public IActionResult OnGet(string code = null, string email = null)
        {
            if (code == null)
            {
                _logger.LogWarning("⚠️ [ResetPassword] Token faltante en URL");
                ModelState.AddModelError(string.Empty, "Token de reseteo faltante.");
                return BadRequest("Se requiere un código para restablecer la contraseña.");
            }

            // ✅ NO decodificar aquí - el code viene correctamente codificado
            Input = new InputModel
            {
                Code = code,
                Email = email ?? "" // Pre-llenar email si viene en URL
            };

            _logger.LogInformation("📧 [ResetPassword] Página cargada para email: {Email}", email ?? "no-especificado");
            
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("⚠️ [ResetPassword] ModelState inválido");
                return Page();
            }

            var user = await _userManager.FindByEmailAsync(Input.Email);
            if (user == null)
            {
                _logger.LogWarning("⚠️ [ResetPassword] Usuario no encontrado: {Email}", Input.Email);
                // No revelar que el usuario no existe
                return RedirectToPage("./ResetPasswordConfirmation");
            }

            try
            {
                // ✅ Decodificar el token SOLO aquí, una vez
                string decodedToken;
                try
                {
                    decodedToken = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(Input.Code));
                    _logger.LogInformation("🔓 [ResetPassword] Token decodificado correctamente");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ [ResetPassword] Error al decodificar token");
                    ModelState.AddModelError(string.Empty, "El token de reseteo es inválido o está corrupto.");
                    return Page();
                }

                // Resetear contraseña
                var result = await _userManager.ResetPasswordAsync(user, decodedToken, Input.Password);

                if (result.Succeeded)
                {
                    _logger.LogInformation("✅ [ResetPassword] Contraseña reseteada exitosamente para: {Email}", Input.Email);
                    return RedirectToPage("./ResetPasswordConfirmation");
                }

                // Loggear errores específicos
                foreach (var error in result.Errors)
                {
                    _logger.LogWarning("⚠️ [ResetPassword] Error: {Code} - {Description}", error.Code, error.Description);
                    
                    // Mensajes más amigables
                    if (error.Code == "InvalidToken")
                    {
                        ModelState.AddModelError(string.Empty, 
                            "El enlace de recuperación ha expirado o ya fue usado. Por favor, solicita uno nuevo.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ [ResetPassword] Excepción al resetear contraseña para: {Email}", Input.Email);
                ModelState.AddModelError(string.Empty, "Ocurrió un error al restablecer tu contraseña. Por favor, intenta de nuevo.");
            }

            return Page();
        }
    }
}
```

---

## 📝 Program.cs (AGREGAR)

**Buscar la línea donde se configura Identity (aproximadamente línea 120-140):**

```csharp
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    // ... configuraciones existentes
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// ⭐ AGREGAR ESTAS LÍNEAS DESPUÉS:

// Configurar Data Protection (para tokens persistentes)
var dataProtectionPath = Path.Combine(builder.Environment.ContentRootPath, "DataProtectionKeys");
Directory.CreateDirectory(dataProtectionPath); // Crear si no existe

builder.Services.AddDataProtection()
    .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionPath))
    .SetApplicationName("eiibd26")
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Keys duran 90 días

// Configurar tiempo de vida de tokens de reseteo
builder.Services.Configure<DataProtectionTokenProviderOptions>(options =>
{
    options.TokenLifespan = TimeSpan.FromDays(1); // 24 horas
});
```

---

## 🧪 Testing

### 1. Aplicar cambios y reiniciar app:
```powershell
# Detener app (Shift+F5)
# Aplicar cambios en código
# Crear carpeta para keys
New-Item -ItemType Directory -Path "eiibd26\DataProtectionKeys" -Force

# Reiniciar app (F5)
```

### 2. Probar recuperación de contraseña:
1. Ir a: `https://localhost:7002/Identity/Account/ForgotPassword`
2. Ingresar email
3. Click en enlace del email
4. Ingresar nueva contraseña
5. **Debería funcionar sin error** ✅

### 3. Verificar logs:
- Output Window → Buscar `[ResetPassword]`
- Debería ver:
  ```
  📧 [ResetPassword] Página cargada para email: ...
  🔓 [ResetPassword] Token decodificado correctamente
  ✅ [ResetPassword] Contraseña reseteada exitosamente
  ```

---

## ⚠️ Si Sigue Fallando

### Test de Token Manualmente:

```csharp
// Agregar endpoint temporal en Program.cs (SOLO DESARROLLO)
#if DEBUG
app.MapGet("/test-reset-token", async (
    UserManager<ApplicationUser> userManager,
    ILogger<Program> logger) =>
{
    var user = await userManager.FindByEmailAsync("tu-email@ejemplo.com");
    if (user == null) return Results.NotFound("Usuario no encontrado");
    
    var token = await userManager.GeneratePasswordResetTokenAsync(user);
    var encoded = WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));
    
    logger.LogInformation("Token original: {Token}", token);
    logger.LogInformation("Token codificado: {Encoded}", encoded);
    
    // Test decodificación
    var decoded = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encoded));
    var matches = token == decoded;
    
    logger.LogInformation("¿Coinciden? {Matches}", matches);
    
    return Results.Ok(new { token, encoded, decoded, matches });
}).RequireAuthorization();
#endif
```

---

## 🎯 Checklist de Solución

- [ ] Corregir `ResetPassword.cshtml.cs` (quitar decodificación en OnGet)
- [ ] Agregar logging en `ResetPassword.cshtml.cs`
- [ ] Configurar Data Protection en `Program.cs`
- [ ] Crear carpeta `DataProtectionKeys`
- [ ] Agregar `DataProtectionKeys/` a `.gitignore`
- [ ] Reiniciar aplicación
- [ ] Probar flujo completo
- [ ] Verificar logs

---

## 📊 Causas Comunes del Error

| Causa | Solución |
|-------|----------|
| **Doble codificación** | ✅ Decodificar solo en OnPostAsync |
| **Keys no persistentes** | ✅ Usar `PersistKeysToFileSystem` |
| **Token expirado** | ✅ Extender `TokenLifespan` |
| **App reiniciada** | ✅ Persistir keys en disco |
| **Token corrupto en URL** | ✅ Verificar codificación |

---

**Aplica estos cambios y el error debería resolverse.** 🚀

Si sigue fallando, comparte:
1. Logs de Output Window con `[ResetPassword]`
2. ¿Cuánto tiempo pasó desde que se generó el token?
3. ¿La app se reinició entre generar y usar el token?
