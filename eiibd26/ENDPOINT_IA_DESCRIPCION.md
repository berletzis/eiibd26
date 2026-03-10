# API Endpoint para Generar Descripción IA de Síntomas/Tratamientos

## 1. ESTRUCTURA DE SOLICITUD

### Endpoint
```
POST /api/admin/sintomas/{id}/generate-ia-description
POST /api/admin/tratamientos/{id}/generate-ia-description
```

### Request Body
```json
{
    "nombre": "Nombre del síntoma/tratamiento",
    "tipo": "sintoma" // o "tratamiento"
}
```

### Response
```json
{
    "ok": true,
    "descripcion": "Descripción generada por IA...",
    "relacionEII": "Sí, documentada relación con EII" | "No se encontró relación documentada"
}
```

---

## 2. PROMPT PARA SÍNTOMAS

El siguiente prompt se envía a Claude API:

```
Actúa como redactor de contenido médico orientado a pacientes no como médico ni como enciclopedia clínica.

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

Ahora genera el contenido para el siguiente síntoma: [NOMBRE_SINTOMA]

ADEMÁS, determina si este síntoma tiene relación documentada con la Enfermedad Inflamatoria Intestinal (EII). Responde SOLO con "SÍ" o "NO" al final del texto, en una línea separada.
```

---

## 3. PROMPT PARA TRATAMIENTOS

```
Actúa como redactor de contenido médico orientado a pacientes, no como médico ni como enciclopedia clínica.

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

Ahora genera el contenido para el siguiente tratamiento: [NOMBRE_TRATAMIENTO]

ADEMÁS, determina si este tratamiento tiene relación documentada con la Enfermedad Inflamatoria Intestinal (EII). Responde SOLO con "SÍ" o "NO" al final del texto, en una línea separada.
```

---

## 4. ESTRUCTURA DEL ENDPOINT EN C#

```csharp
[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Administrador")]
public class AdminSintomasTratamientosController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly IClaudeAiService _claudeService;
    private readonly ILogger<AdminSintomasTratamientosController> _logger;

    public AdminSintomasTratamientosController(
        ApplicationDbContext db,
        IClaudeAiService claudeService,
        ILogger<AdminSintomasTratamientosController> logger)
    {
        _db = db;
        _claudeService = claudeService;
        _logger = logger;
    }

    [HttpPost("sintomas/{id}/generate-ia-description")]
    public async Task<IActionResult> GenerateSintomaDescription(int id)
    {
        try
        {
            var sintoma = await _db.sintomas.FindAsync(id);
            if (sintoma == null)
                return NotFound(new { ok = false, error = "Síntoma no encontrado" });

            // Prompt para síntomas
            var prompt = $$"""
                Actúa como redactor de contenido médico orientado a pacientes...
                
                Ahora genera el contenido para el siguiente síntoma: {{sintoma.nombre}}
                """;

            // Llamar a Claude API
            var response = await _claudeService.GenerateContentAsync(prompt);

            // Procesar respuesta
            var lines = response.Split(new[] { "\n" }, StringSplitOptions.None);
            var descripcion = string.Join("\n", lines.Take(lines.Length - 1));
            var relacionEII = lines.Last().Trim().Equals("SÍ", StringComparison.OrdinalIgnoreCase);

            // Guardar en base de datos
            sintoma.DescripcionIA = descripcion;
            sintoma.ValidadoIA = true;
            sintoma.RelacionEII = relacionEII ? "Sí, documentada relación con EII" : "No se encontró relación documentada";
            sintoma.FechaActualizacionIA = DateTime.UtcNow;

            await _db.SaveChangesAsync();

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
            return StatusCode(500, new { ok = false, error = "Error al generar descripción" });
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

            // Prompt para tratamientos
            var prompt = $$"""
                Actúa como redactor de contenido médico orientado a pacientes...
                
                Ahora genera el contenido para el siguiente tratamiento: {{tratamiento.nombre}}
                """;

            // Llamar a Claude API
            var response = await _claudeService.GenerateContentAsync(prompt);

            // Procesar respuesta
            var lines = response.Split(new[] { "\n" }, StringSplitOptions.None);
            var descripcion = string.Join("\n", lines.Take(lines.Length - 1));
            var relacionEII = lines.Last().Trim().Equals("SÍ", StringComparison.OrdinalIgnoreCase);

            // Guardar en base de datos
            tratamiento.DescripcionIA = descripcion;
            tratamiento.ValidadoIA = true;
            tratamiento.RelacionEII = relacionEII ? "Sí, documentada relación con EII" : "No se encontró relación documentada";
            tratamiento.FechaActualizacionIA = DateTime.UtcNow;

            await _db.SaveChangesAsync();

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
            return StatusCode(500, new { ok = false, error = "Error al generar descripción" });
        }
    }
}
```

---

## 5. INTEGRACIÓN CON EL FORMULARIO (JavaScript)

```javascript
// En el formulario de edición de síntomas/tratamientos

document.getElementById('btnGenerarDescripcionIA').addEventListener('click', async function() {
    const id = document.getElementById('itemId').value;
    const tipo = document.getElementById('itemTipo').value; // 'sintoma' o 'tratamiento'
    
    // Mostrar cargando
    this.disabled = true;
    this.innerHTML = '<i class="bi bi-hourglass-split"></i> Generando...';
    
    try {
        const response = await fetch(`/api/admin/${tipo}s/${id}/generate-ia-description`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        });
        
        const data = await response.json();
        
        if (data.ok) {
            // Llenar el campo de descripción IA
            document.getElementById('DescripcionIA').value = data.descripcion;
            document.getElementById('ValidadoIA').checked = true;
            document.getElementById('RelacionEII').value = data.relacionEII;
            
            // Mostrar éxito
            alert('✅ Descripción generada exitosamente');
        } else {
            alert('❌ Error: ' + data.error);
        }
    } catch (error) {
        console.error('Error:', error);
        alert('❌ Error de conexión');
    } finally {
        this.disabled = false;
        this.innerHTML = '<i class="bi bi-sparkles"></i> Generar Descripción IA';
    }
});
```

---

## 6. PRÓXIMOS PASOS

1. ✅ Crear los modelos C#
2. ✅ Crear las migraciones EF Core
3. ⏳ Implementar el endpoint API
4. ⏳ Actualizar el formulario (cambiar Modal a Panel lateral)
5. ⏳ Agregar el botón "Generar Descripción IA"
6. ⏳ Mostrar campos nuevos en el Grid

