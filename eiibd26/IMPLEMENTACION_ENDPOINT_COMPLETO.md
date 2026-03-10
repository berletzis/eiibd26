# IMPLEMENTACIÓN COMPLETA - Endpoint API para IA

## Crear archivo: `eiibd26/Controllers/AdminSintomasTratamientosApiController.cs`

```csharp
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using eiibd26.Data;
using eiibd26.Models;
using eiibd26.Services;

namespace eiibd26.Controllers
{
    [ApiController]
    [Route("api/admin")]
    [Authorize(Roles = "Administrador")]
    public class AdminSintomasTratamientosApiController : ControllerBase
    {
        private readonly ApplicationDbContext _db;
        private readonly IClaudeAiService _claudeService;
        private readonly ILogger<AdminSintomasTratamientosApiController> _logger;

        public AdminSintomasTratamientosApiController(
            ApplicationDbContext db,
            IClaudeAiService claudeService,
            ILogger<AdminSintomasTratamientosApiController> logger)
        {
            _db = db;
            _claudeService = claudeService;
            _logger = logger;
        }

        // ===== GET ENDPOINTS =====

        [HttpGet("sintomas/{id}")]
        public async Task<IActionResult> GetSintoma(int id)
        {
            try
            {
                var sintoma = await _db.sintomas.FindAsync(id);
                if (sintoma == null)
                    return NotFound(new { ok = false, error = "Síntoma no encontrado" });

                return Ok(new
                {
                    ok = true,
                    data = new
                    {
                        id = sintoma.id,
                        nombre = sintoma.nombre,
                        icono = sintoma.icono,
                        descripcionIA = sintoma.DescripcionIA,
                        validadoIA = sintoma.ValidadoIA,
                        validadoHumano = sintoma.ValidadoHumano,
                        relacionEII = sintoma.RelacionEII,
                        fechaActualizacionIA = sintoma.FechaActualizacionIA
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener síntoma {id}", id);
                return StatusCode(500, new { ok = false, error = "Error al obtener datos" });
            }
        }

        [HttpGet("tratamientos/{id}")]
        public async Task<IActionResult> GetTratamiento(int id)
        {
            try
            {
                var tratamiento = await _db.tratamientos.FindAsync(id);
                if (tratamiento == null)
                    return NotFound(new { ok = false, error = "Tratamiento no encontrado" });

                return Ok(new
                {
                    ok = true,
                    data = new
                    {
                        id = tratamiento.id,
                        nombre = tratamiento.nombre,
                        icono = tratamiento.icono,
                        descripcionIA = tratamiento.DescripcionIA,
                        validadoIA = tratamiento.ValidadoIA,
                        validadoHumano = tratamiento.ValidadoHumano,
                        relacionEII = tratamiento.RelacionEII,
                        fechaActualizacionIA = tratamiento.FechaActualizacionIA
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener tratamiento {id}", id);
                return StatusCode(500, new { ok = false, error = "Error al obtener datos" });
            }
        }

        // ===== PUT ENDPOINTS (Guardar) =====

        [HttpPut("sintomas/{id}")]
        public async Task<IActionResult> UpdateSintoma(int id, [FromBody] UpdateSintomaRequest request)
        {
            try
            {
                var sintoma = await _db.sintomas.FindAsync(id);
                if (sintoma == null)
                    return NotFound(new { ok = false, error = "Síntoma no encontrado" });

                sintoma.nombre = request.nombre ?? sintoma.nombre;
                sintoma.icono = request.icono ?? sintoma.icono;
                sintoma.DescripcionIA = request.descripcionIA ?? sintoma.DescripcionIA;
                sintoma.ValidadoIA = request.validadoIA;
                sintoma.ValidadoHumano = request.validadoHumano;
                sintoma.RelacionEII = request.relacionEII ?? sintoma.RelacionEII;
                sintoma.fechaModificado = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new { ok = true, message = "Síntoma actualizado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar síntoma {id}", id);
                return StatusCode(500, new { ok = false, error = "Error al guardar datos" });
            }
        }

        [HttpPut("tratamientos/{id}")]
        public async Task<IActionResult> UpdateTratamiento(int id, [FromBody] UpdateTratamientoRequest request)
        {
            try
            {
                var tratamiento = await _db.tratamientos.FindAsync(id);
                if (tratamiento == null)
                    return NotFound(new { ok = false, error = "Tratamiento no encontrado" });

                tratamiento.nombre = request.nombre ?? tratamiento.nombre;
                tratamiento.icono = request.icono ?? tratamiento.icono;
                tratamiento.DescripcionIA = request.descripcionIA ?? tratamiento.DescripcionIA;
                tratamiento.ValidadoIA = request.validadoIA;
                tratamiento.ValidadoHumano = request.validadoHumano;
                tratamiento.RelacionEII = request.relacionEII ?? tratamiento.RelacionEII;
                tratamiento.fechaModificado = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                return Ok(new { ok = true, message = "Tratamiento actualizado correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar tratamiento {id}", id);
                return StatusCode(500, new { ok = false, error = "Error al guardar datos" });
            }
        }

        // ===== IA GENERATION ENDPOINTS =====

        [HttpPost("sintomas/{id}/generate-ia-description")]
        public async Task<IActionResult> GenerateSintomaDescription(int id)
        {
            try
            {
                var sintoma = await _db.sintomas.FindAsync(id);
                if (sintoma == null)
                    return NotFound(new { ok = false, error = "Síntoma no encontrado" });

                // Construir prompt completo
                var prompt = BuildSintomaPrompt(sintoma.nombre);

                // Llamar a Claude API
                var response = await _claudeService.GenerateContentAsync(prompt);

                // Procesar respuesta: última línea es "SÍ" o "NO"
                var lines = response.Split(new[] { "\n" }, StringSplitOptions.None);
                var lastLine = lines.Length > 0 ? lines[^1].Trim() : "";
                
                // La descripción es todo excepto la última línea
                var descripcion = string.Join("\n", lines, 0, Math.Max(0, lines.Length - 1)).Trim();
                
                var relacionEII = lastLine.Equals("SÍ", StringComparison.OrdinalIgnoreCase);

                // Guardar en base de datos
                sintoma.DescripcionIA = descripcion;
                sintoma.ValidadoIA = true;
                sintoma.RelacionEII = relacionEII ? "Sí, documentada relación con EII" : "No se encontró relación documentada";
                sintoma.FechaActualizacionIA = DateTime.UtcNow;
                sintoma.fechaModificado = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Descripción IA generada para síntoma {id}", id);

                return Ok(new
                {
                    ok = true,
                    descripcion = descripcion,
                    relacionEII = sintoma.RelacionEII
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar descripción IA para síntoma {id}", id);
                return StatusCode(500, new { ok = false, error = "Error al generar descripción. Intenta de nuevo." });
            }
        }

        [HttpPost("tratamientos/{id}/generate-ia-description")]
        public async Task<IActionResult> GenerateTratamientoDescription(int id)
        {
            try
            {
                var tratamiento = await _db.tratamientos.FindAsync(id);
                if (tratamiento == null)
                    return NotFound(new { ok = false, error = "Tratamiento no encontrado" });

                // Construir prompt completo
                var prompt = BuildTratamientoPrompt(tratamiento.nombre);

                // Llamar a Claude API
                var response = await _claudeService.GenerateContentAsync(prompt);

                // Procesar respuesta: última línea es "SÍ" o "NO"
                var lines = response.Split(new[] { "\n" }, StringSplitOptions.None);
                var lastLine = lines.Length > 0 ? lines[^1].Trim() : "";
                
                // La descripción es todo excepto la última línea
                var descripcion = string.Join("\n", lines, 0, Math.Max(0, lines.Length - 1)).Trim();
                
                var relacionEII = lastLine.Equals("SÍ", StringComparison.OrdinalIgnoreCase);

                // Guardar en base de datos
                tratamiento.DescripcionIA = descripcion;
                tratamiento.ValidadoIA = true;
                tratamiento.RelacionEII = relacionEII ? "Sí, documentada relación con EII" : "No se encontró relación documentada";
                tratamiento.FechaActualizacionIA = DateTime.UtcNow;
                tratamiento.fechaModificado = DateTime.UtcNow;

                await _db.SaveChangesAsync();

                _logger.LogInformation("Descripción IA generada para tratamiento {id}", id);

                return Ok(new
                {
                    ok = true,
                    descripcion = descripcion,
                    relacionEII = tratamiento.RelacionEII
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar descripción IA para tratamiento {id}", id);
                return StatusCode(500, new { ok = false, error = "Error al generar descripción. Intenta de nuevo." });
            }
        }

        // ===== HELPER METHODS =====

        private static string BuildSintomaPrompt(string nombreSintoma)
        {
            return $@"Actúa como redactor de contenido médico orientado a pacientes, no como médico ni como enciclopedia clínica.

Tu objetivo es describir síntomas en lenguaje sencillo para ayudar a las personas a reconocer y expresar lo que sienten, SIN diagnosticar ni explicar enfermedades en profundidad.

Reglas obligatorias:

- Usa lenguaje claro y cotidiano (nivel lectura 6–8 grado).
- NO expliques mecanismos biológicos complejos.
- NO menciones tratamientos.
- NO sugieras diagnósticos.
- NO listes causas específicas graves.
- Describe solo la EXPERIENCIA del síntoma.
- Mantén tono neutral y tranquilizador.
- Máximo 120 palabras totales.

Estructura EXACTA:

Nombre del síntoma (traducción simple)

¿Qué es?
Explicación breve en 1–2 frases centradas en cómo se siente.

¿Cómo puede sentirse?
• 4 ejemplos cotidianos reales del paciente.

Importante
Aclara que es un síntoma y no un diagnóstico y que requiere evaluación médica si persiste.

Ahora genera el contenido para el siguiente síntoma: {nombreSintoma}

ADEMÁS, determina si este síntoma tiene relación documentada con la Enfermedad Inflamatoria Intestinal (EII). Responde SOLO con ""SÍ"" o ""NO"" al final del texto, en una línea separada.";
        }

        private static string BuildTratamientoPrompt(string nombreTratamiento)
        {
            return $@"Actúa como redactor de contenido médico orientado a pacientes, no como médico ni como enciclopedia clínica.

Tu objetivo es describir tratamientos en lenguaje sencillo para ayudar a las personas a entender qué son y cómo funcionan de forma general, SIN profundizar en mecanismos biológicos complejos.

Reglas obligatorias:

- Usa lenguaje claro y cotidiano (nivel lectura 6–8 grado).
- NO expliques mecanismos biológicos muy complejos.
- Describe el PROPÓSITO y FORMA DE USO general.
- NO sugieras que es la solución definitiva.
- Mantén tono neutral y tranquilizador.
- Máximo 120 palabras totales.

Estructura EXACTA:

Nombre del tratamiento (traducción simple si es necesario)

¿Qué es?
Explicación breve en 1–2 frases sobre su propósito.

¿Cómo se usa?
• 3-4 ejemplos de formas comunes de administración/uso.

Importante
Aclara que debe seguir recomendaciones médicas y que todo tratamiento requiere supervisión profesional.

Ahora genera el contenido para el siguiente tratamiento: {nombreTratamiento}

ADEMÁS, determina si este tratamiento tiene relación documentada con la Enfermedad Inflamatoria Intestinal (EII). Responde SOLO con ""SÍ"" o ""NO"" al final del texto, en una línea separada.";
        }
    }

    // ===== REQUEST MODELS =====

    public class UpdateSintomaRequest
    {
        public string nombre { get; set; }
        public string icono { get; set; }
        public string descripcionIA { get; set; }
        public bool validadoIA { get; set; }
        public bool validadoHumano { get; set; }
        public string relacionEII { get; set; }
    }

    public class UpdateTratamientoRequest
    {
        public string nombre { get; set; }
        public string icono { get; set; }
        public string descripcionIA { get; set; }
        public bool validadoIA { get; set; }
        public bool validadoHumano { get; set; }
        public string relacionEII { get; set; }
    }
}
```

## Registrar el Controller en Startup/Program.cs

Si usas `Program.cs` (.NET 6+):

```csharp
builder.Services.AddControllers();

// Agregar:
builder.Services.AddAuthorization();
```

Si usas `Startup.cs` (.NET Core):

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();
    services.AddAuthorization();
}
```

---

## NOTAS IMPORTANTES

1. **Reutilización de Claude API**
   - El código usa `IClaudeAiService` existente
   - No necesita nueva configuración

2. **Validación de permisos**
   - `[Authorize(Roles = "Administrador")]` protege todos los endpoints

3. **Manejo de errores**
   - Logging completo para debugging
   - Respuestas JSON consistentes

4. **Respuestas de éxito**
   ```json
   {
     "ok": true,
     "descripcion": "...",
     "relacionEII": "Sí, documentada relación con EII"
   }
   ```

5. **Respuestas de error**
   ```json
   {
     "ok": false,
     "error": "Error al generar descripción"
   }
   ```

