# UI para Respuesta de IA - Implementación Completa

## 📋 Resumen de Cambios

Se implementó una interfaz de usuario mejorada para la respuesta de IA con las siguientes características:

### ✅ Características Implementadas

1. **Posicionamiento**: La respuesta de IA aparece abajo del card de la pregunta y arriba del bloque de respuestas de usuarios
2. **Toggle colapsable**: La respuesta está en un toggle que se puede abrir/cerrar
3. **Comportamiento inteligente**: 
   - Abierto cuando hay menos de 3 respuestas humanas
   - Cerrado cuando hay 3+ respuestas humanas
   - La cápsula "Respuesta Informativa (IA)" siempre visible
4. **Indicador de procesamiento**: Muestra "Procesando tu pregunta..." mientras la IA está generando la respuesta
5. **Auto-actualización**: Polling automático cada 5 segundos para detectar cuando la respuesta está lista

---

## 🎨 Componentes UI Implementados

### 1. Indicador de "Procesando..."

Aparece cuando:
- La pregunta tiene menos de 5 minutos de antigüedad
- Aún no tiene respuesta de IA

**HTML:**
```html
<div class="ai-processing-indicator" id="aiProcessingIndicator">
    <div class="ai-processing-content">
        <div class="ai-processing-spinner"></div>
        <span class="ai-processing-text">Procesando tu pregunta...</span>
    </div>
</div>
```

**CSS:**
- Gradiente morado suave de fondo
- Spinner animado
- Texto en color brand (#764ba2)

### 2. Toggle de Respuesta IA

**HTML:**
```html
<div class="ai-answer-toggle-wrapper ai-toggle-open" id="aiAnswerToggle">
    <div class="ai-toggle-header" onclick="toggleAiAnswer()">
        <span class="ai-toggle-badge">🤖 Respuesta Informativa (IA)</span>
        <span class="ai-toggle-icon"><i class="bi bi-chevron-down"></i></span>
    </div>
    <div class="ai-toggle-content">
        <!-- Contenido de la respuesta -->
    </div>
</div>
```

**CSS:**
- Header con gradiente morado
- Animación suave de expansión/colapso
- Icono que rota 180° al abrir/cerrar
- Hover effects
- Borde destacado en color brand

### 3. Separador Visual

Entre la respuesta de IA y las respuestas de usuarios:

```html
<div style="margin: 2rem 0; padding: 1rem 0; border-bottom: 2px solid #e5e7eb;">
    <h3>💬 Respuestas de la comunidad (N)</h3>
</div>
```

---

## 💻 Cambios en el Código

### 📄 **Detalles.cshtml.cs** (CodeBehind)

#### Nuevas propiedades agregadas:

```csharp
public RespuestaVm RespuestaIA { get; set; }
public bool TienePreguntaPendienteIA { get; set; } = false;
public int TotalRespuestasHumanas { get; set; } = 0;
```

#### Nueva lógica en OnGetAsync:

```csharp
// Cargar respuesta de IA
var preguntaConIA = await _db.Preguntas.AsNoTracking()
    .Where(p => p.Id == preguntaId)
    .Select(p => new { p.TieneRespuestaIA, p.FechaGeneracionIA })
    .FirstOrDefaultAsync();

if (preguntaConIA.TieneRespuestaIA)
{
    // Cargar la respuesta de IA...
    RespuestaIA = ...;
}
else
{
    // Verificar si está pendiente (menos de 5 minutos)
    var minutosDesdeCreacion = (DateTimeOffset.UtcNow - preguntaFechaCreacion).TotalMinutes;
    TienePreguntaPendienteIA = minutosDesdeCreacion < 5;
}

// Contar respuestas humanas (excluir IA)
TotalRespuestasHumanas = await _db.Respuestas.AsNoTracking()
    .Where(r => r.PreguntaId == preguntaId && !r.Eliminado && !r.EsIA)
    .CountAsync();
```

### 📄 **Detalles.cshtml** (Vista Razor)

#### Estructura del HTML:

```
1. Article de la Pregunta
   └─ [NUEVO] Indicador "Procesando..." (si aplica)
   └─ [NUEVO] Toggle de Respuesta IA (si existe)
   └─ [NUEVO] Separador visual
2. Reply Box (formulario para responder)
3. Accepted Answer (si existe)
4. Lista de Respuestas de Usuarios (paginadas)
```

#### Filtrado de respuestas:

```csharp
var filteredRespuestas = Model.Respuestas
    .Where(r => !(Model.AcceptedAnswer != null && r.Id == Model.AcceptedAnswer.Id))
    .Where(r => !(Model.RespuestaIA != null && r.Id == Model.RespuestaIA.Id))
    .ToList();
```

La respuesta de IA **NO aparece en la lista normal** porque tiene su propio toggle especial.

### 📄 **PreguntasApiController.cs**

#### Nuevo endpoint para polling:

```csharp
[HttpGet("{id:guid}/ai-status")]
[AllowAnonymous]
public async Task<IActionResult> GetAiStatus(Guid id)
{
    var pregunta = await _db.Preguntas.AsNoTracking()
        .Where(p => p.Id == id && !p.Eliminado)
        .Select(p => new { p.TieneRespuestaIA, p.FechaGeneracionIA })
        .FirstOrDefaultAsync();

    return Ok(new
    {
        ok = true,
        hasAiAnswer = pregunta.TieneRespuestaIA,
        generatedAt = pregunta.FechaGeneracionIA,
        respuestaId = ...
    });
}
```

**Propósito**: Permite al frontend verificar si la respuesta de IA ya está lista sin recargar toda la página.

---

## 🎯 Lógica de Toggle

### Estado Inicial

El toggle se abre o cierra según el número de respuestas humanas:

```csharp
var shouldBeOpen = Model.TotalRespuestasHumanas < 3;
var toggleClass = shouldBeOpen ? "ai-toggle-open" : "ai-toggle-closed";
```

**Casos:**
- **0-2 respuestas humanas**: Toggle ABIERTO (la IA ayuda mientras llegan más respuestas)
- **3+ respuestas humanas**: Toggle CERRADO (priorizar respuestas de la comunidad)

### Interacción del Usuario

El usuario puede abrir/cerrar manualmente el toggle en cualquier momento:

```javascript
window.toggleAiAnswer = function() {
    const wrapper = document.getElementById('aiAnswerToggle');
    if (wrapper.classList.contains('ai-toggle-open')) {
        wrapper.classList.remove('ai-toggle-open');
        wrapper.classList.add('ai-toggle-closed');
    } else {
        wrapper.classList.remove('ai-toggle-closed');
        wrapper.classList.add('ai-toggle-open');
    }
};
```

**CSS Transitions:**
- `max-height: 0` cuando cerrado → `max-height: 5000px` cuando abierto
- Transición suave de 0.4s
- Icono rota 180° con transición de 0.3s

---

## 🔄 Sistema de Polling

### Objetivo

Detectar automáticamente cuando la respuesta de IA está lista y recargar la página para mostrarla.

### Implementación

```javascript
const pollForAiAnswer = async () => {
    if (pollCount >= maxPolls) {
        console.log('⏰ Polling timeout alcanzado');
        return;
    }
    
    const response = await fetch(`/api/preguntas/${preguntaId}/ai-status`);
    const data = await response.json();
    
    if (data.ok && data.hasAiAnswer) {
        console.log('✅ Respuesta de IA detectada, recargando...');
        window.location.reload();
        return;
    }
    
    // Continuar polling cada 5 segundos
    setTimeout(pollForAiAnswer, 5000);
};

// Iniciar después de 5 segundos
setTimeout(pollForAiAnswer, 5000);
```

**Parámetros:**
- **Intervalo**: 5 segundos entre cada verificación
- **Máximo intentos**: 60 (5 minutos total)
- **Delay inicial**: 5 segundos (dar tiempo a que el job inicie)

**Optimización:**
- Usa el endpoint `/api/preguntas/{id}/ai-status` que solo devuelve JSON ligero
- NO recarga el HTML completo en cada poll
- Solo recarga la página cuando detecta que la respuesta está lista

---

## 🎨 Estilos CSS Detallados

### Indicador de Procesamiento

```css
.ai-processing-indicator {
    margin: 1.5rem 0;
    padding: 1.25rem 1.5rem;
    background: linear-gradient(135deg, rgba(102, 126, 234, 0.08) 0%, rgba(118, 75, 162, 0.08) 100%);
    border: 1px solid rgba(118, 75, 162, 0.2);
    border-radius: 12px;
    box-shadow: 0 2px 8px rgba(118, 75, 162, 0.1);
}

.ai-processing-spinner {
    width: 24px;
    height: 24px;
    border: 3px solid rgba(118, 75, 162, 0.2);
    border-top-color: #764ba2;
    border-radius: 50%;
    animation: spin 1s linear infinite;
}
```

### Toggle Header

```css
.ai-toggle-header {
    padding: 1rem 1.5rem;
    background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
    cursor: pointer;
    display: flex;
    justify-content: space-between;
    align-items: center;
    user-select: none;
}

.ai-toggle-header:hover {
    background: linear-gradient(135deg, #5568d3 0%, #653a8b 100%);
}

.ai-toggle-header:active {
    transform: scale(0.99);
}
```

### Animación del Icono

```css
.ai-toggle-icon {
    color: white;
    font-size: 1.2rem;
    transition: transform 0.3s ease;
}

.ai-toggle-open .ai-toggle-icon {
    transform: rotate(180deg);
}
```

### Contenido Colapsable

```css
.ai-toggle-content {
    max-height: 0;
    overflow: hidden;
    transition: max-height 0.4s ease;
}

.ai-toggle-open .ai-toggle-content {
    max-height: 5000px;
}
```

---

## 🧪 Casos de Prueba

### Caso 1: Pregunta Nueva (Procesando)

**Condiciones:**
- Pregunta tiene menos de 5 minutos
- No tiene `TieneRespuestaIA = true`
- No existe respuesta con `EsIA = true`

**Resultado esperado:**
```
1. [Card de la Pregunta]
2. [Indicador "Procesando tu pregunta..." con spinner]
3. [Reply Box]
4. [Lista de respuestas (vacía o con respuestas humanas)]
```

**Comportamiento:**
- Polling cada 5 segundos verificando `/api/preguntas/{id}/ai-status`
- Cuando detecta `hasAiAnswer: true` → recarga la página

### Caso 2: Pregunta con IA, Pocas Respuestas (<3)

**Condiciones:**
- Pregunta tiene `TieneRespuestaIA = true`
- Tiene 0-2 respuestas humanas
- Existe respuesta con `EsIA = true`

**Resultado esperado:**
```
1. [Card de la Pregunta]
2. [Toggle ABIERTO con respuesta de IA visible]
   ┌─────────────────────────────────┐
   │ 🤖 Respuesta Informativa (IA) ⌄ │ ← Header clickeable
   ├─────────────────────────────────┤
   │ [Contenido de la respuesta]     │ ← Visible
   │ [Botones de voto]               │
   │ [Metadata: modelo, fecha]       │
   └─────────────────────────────────┘
3. [Separador: "💬 Respuestas de la comunidad (2)"]
4. [Reply Box]
5. [Lista de respuestas humanas (0-2 respuestas)]
```

### Caso 3: Pregunta con IA, Muchas Respuestas (≥3)

**Condiciones:**
- Pregunta tiene `TieneRespuestaIA = true`
- Tiene 3+ respuestas humanas

**Resultado esperado:**
```
1. [Card de la Pregunta]
2. [Toggle CERRADO, solo se ve el header]
   ┌─────────────────────────────────┐
   │ 🤖 Respuesta Informativa (IA) ⌄ │ ← Header clickeable
   └─────────────────────────────────┘
   (contenido oculto)
3. [Separador: "💬 Respuestas de la comunidad (5)"]
4. [Reply Box]
5. [Lista de respuestas humanas (paginadas)]
```

**Comportamiento:**
- Usuario puede hacer clic en el header para expandir
- El toggle guarda su estado en la sesión del navegador (via clases CSS)

### Caso 4: Pregunta Antigua sin IA

**Condiciones:**
- Pregunta tiene más de 5 minutos
- No tiene respuesta de IA

**Resultado esperado:**
```
1. [Card de la Pregunta]
2. [Reply Box]
3. [Lista de respuestas humanas]
```

Sin indicador de procesamiento (asume que el job falló o no se ejecutó).

---

## 🔧 Archivos Modificados

### 1. **Pages/Preguntas/Detalles.cshtml.cs**

**Líneas modificadas: ~90-140**

```csharp
// Nuevas propiedades
public RespuestaVm RespuestaIA { get; set; }
public bool TienePreguntaPendienteIA { get; set; } = false;
public int TotalRespuestasHumanas { get; set; } = 0;

// Lógica en OnGetAsync (antes del return Page();)
var preguntaConIA = await _db.Preguntas.AsNoTracking()
    .Where(p => p.Id == preguntaId)
    .Select(p => new { p.TieneRespuestaIA, p.FechaGeneracionIA })
    .FirstOrDefaultAsync();

if (preguntaConIA.TieneRespuestaIA)
{
    // Cargar respuesta de IA
    RespuestaIA = await LoadAiAnswer(...);
}
else
{
    // Verificar si está pendiente
    var minutosDesdeCreacion = (DateTimeOffset.UtcNow - preguntaFechaCreacion).TotalMinutes;
    TienePreguntaPendienteIA = minutosDesdeCreacion < 5;
}

TotalRespuestasHumanas = await _db.Respuestas.AsNoTracking()
    .Where(r => r.PreguntaId == preguntaId && !r.Eliminado && !r.EsIA)
    .CountAsync();
```

### 2. **Pages/Preguntas/Detalles.cshtml**

**Sección Styles** (líneas ~230-340):
- Agregados estilos para `.ai-processing-indicator`
- Agregados estilos para `.ai-answer-toggle-wrapper`
- Agregados estilos para `.ai-toggle-header`, `.ai-toggle-content`
- Agregadas animaciones para spinner y expansión

**HTML** (después del article de la pregunta):
```razor
@if (Model.TienePreguntaPendienteIA && Model.RespuestaIA == null)
{
    <div class="ai-processing-indicator" id="aiProcessingIndicator">...</div>
}
else if (Model.RespuestaIA != null)
{
    var shouldBeOpen = Model.TotalRespuestasHumanas < 3;
    <div class="ai-answer-toggle-wrapper @(shouldBeOpen ? "ai-toggle-open" : "ai-toggle-closed")">
        ...
    </div>
}

@if (User.Identity.IsAuthenticated && (Model.RespuestaIA != null || Model.TienePreguntaPendienteIA))
{
    <div style="...">
        <h3>💬 Respuestas de la comunidad (@Model.TotalRespuestasHumanas)</h3>
    </div>
}
```

**Scripts** (sección @section Scripts):
```javascript
// Función global para toggle
window.toggleAiAnswer = function() { ... };

// Polling cada 5 segundos
const pollForAiAnswer = async () => {
    const response = await fetch(`/api/preguntas/${preguntaId}/ai-status`);
    if (data.hasAiAnswer) {
        window.location.reload();
    }
};
```

### 3. **Controllers/PreguntasApiController.cs**

**Nuevo endpoint** (después de VotarPregunta):

```csharp
[HttpGet("{id:guid}/ai-status")]
[AllowAnonymous]
public async Task<IActionResult> GetAiStatus(Guid id)
{
    var pregunta = await _db.Preguntas.AsNoTracking()
        .Where(p => p.Id == id && !p.Eliminado)
        .Select(p => new { p.TieneRespuestaIA, p.FechaGeneracionIA })
        .FirstOrDefaultAsync();

    return Ok(new
    {
        ok = true,
        hasAiAnswer = pregunta.TieneRespuestaIA,
        generatedAt = pregunta.FechaGeneracionIA,
        respuestaId = ...
    });
}
```

**Propósito**: Endpoint ligero para polling sin recargar HTML completo.

### 4. **Areas/Identity/Pages/Usuario/usuarioPreguntasRespuestas.cshtml.cs**

**Constructor modificado**:
```csharp
private readonly IServiceProvider _serviceProvider;

public PreguntasRespuestasModel(
    ApplicationDbContext db,
    ILogger<PreguntasRespuestasModel> logger,
    IServiceProvider serviceProvider)
```

**Método OnPostCrearPreguntaAsync**:
- Agregado inicio del job de IA con Task.Factory.StartNew
- Agregados logs detallados
- Mismo patrón que en PreguntasApiController

---

## 🚀 Flujo Completo (End-to-End)

### 1. Usuario crea una pregunta

```
Usuario → [Formulario] → OnPostCrearPreguntaAsync
    ↓
    Guardar en BD
    ↓
    Iniciar Task.Factory.StartNew (AiAnswerJob)
    ↓
    Retornar OK al cliente (inmediato)
```

### 2. Background Job ejecuta

```
Task.Factory.StartNew
    ↓
    Crear scope de DI
    ↓
    Obtener AiAnswerJob
    ↓
    ProcesarPreguntaAsync
        ↓
        Llamar a Claude API
        ↓
        Validar seguridad
        ↓
        Guardar respuesta en BD
        ↓
        Marcar TieneRespuestaIA = true
```

### 3. Usuario ve la pregunta

```
Cargar página de detalles
    ↓
    OnGetAsync verifica:
    ├─ ¿TieneRespuestaIA? → Cargar RespuestaIA
    ├─ ¿Pregunta reciente sin IA? → TienePreguntaPendienteIA = true
    └─ Contar TotalRespuestasHumanas
```

### 4. Renderizado

```
Razor evalúa:
    ├─ Si TienePreguntaPendienteIA && !RespuestaIA
    │   └─ Mostrar indicador "Procesando..."
    │       └─ Iniciar polling cada 5s
    │
    ├─ Si RespuestaIA != null
    │   └─ Mostrar toggle
    │       ├─ Abierto si respuestas humanas < 3
    │       └─ Cerrado si respuestas humanas ≥ 3
    │
    └─ Siempre mostrar lista de respuestas humanas
```

### 5. Polling detecta respuesta

```
JavaScript polling (cada 5s)
    ↓
    Fetch /api/preguntas/{id}/ai-status
    ↓
    Si hasAiAnswer = true
    ↓
    window.location.reload()
    ↓
    Página recarga con toggle visible
```

---

## 📊 Rendimiento

### Optimizaciones implementadas:

1. **Queries separadas**: La respuesta de IA se carga con query independiente (no afecta paginación)
2. **AsNoTracking**: Todas las queries usan `.AsNoTracking()` para mejor performance
3. **Polling ligero**: El endpoint de status devuelve solo 3 propiedades (sin joins pesados)
4. **Timeout del polling**: Se detiene después de 5 minutos para no saturar el servidor

### Impacto en BD:

- **Carga inicial**: +2 queries adicionales (check estado IA + count respuestas humanas)
- **Polling**: 1 query ligera cada 5 segundos durante máximo 5 minutos
- **Costo estimado**: Muy bajo (queries con índices en `PreguntaId` y `EsIA`)

---

## 🎨 Diseño Visual

### Paleta de Colores

- **Brand Primary**: `#764ba2` (morado)
- **Brand Secondary**: `#667eea` (azul-morado)
- **Gradiente Header**: `linear-gradient(135deg, #667eea 0%, #764ba2 100%)`
- **Background IA**: `rgba(118, 75, 162, 0.03)` (muy sutil)

### Iconos

- **IA Badge**: 🤖 (emoji robot)
- **Toggle Icon**: `bi bi-chevron-down` (Bootstrap Icons)
- **Spinner**: CSS puro (border animation)

### Espaciado

- **Margin vertical**: 1.5rem entre secciones
- **Padding interno**: 1rem - 1.5rem según componente
- **Border radius**: 12px (consistente con el resto del diseño)

---

## 🐛 Troubleshooting

### El toggle no aparece

**Verificar:**
1. ¿La pregunta tiene `TieneRespuestaIA = true` en BD?
2. ¿Existe una respuesta con `EsIA = true` para esa pregunta?
3. ¿Los logs muestran que `RespuestaIA` se cargó correctamente?

**Solución:**
```sql
SELECT * FROM Preguntas WHERE Id = '<pregunta-id>'
SELECT * FROM Respuestas WHERE PreguntaId = '<pregunta-id>' AND EsIA = 1
```

### El indicador "Procesando..." no aparece

**Verificar:**
1. ¿La pregunta tiene menos de 5 minutos de antigüedad?
2. ¿`TienePreguntaPendienteIA` es true en el CodeBehind?

**Debug:**
Agregar breakpoint en `Detalles.cshtml.cs` línea donde se calcula:
```csharp
TienePreguntaPendienteIA = minutosDesdeCreacion < 5;
```

### El polling no funciona

**Verificar:**
1. ¿El endpoint `/api/preguntas/{id}/ai-status` responde correctamente?
2. ¿La consola del navegador muestra los logs de polling?

**Test manual:**
```bash
curl https://localhost:7xxx/api/preguntas/<id>/ai-status
```

Debe devolver:
```json
{
  "ok": true,
  "hasAiAnswer": false,
  "generatedAt": null,
  "respuestaId": null
}
```

### El toggle no se abre/cierra

**Verificar:**
1. ¿La función `toggleAiAnswer` está definida globalmente?
2. ¿El `onclick` del header llama correctamente a la función?

**Test en consola:**
```javascript
window.toggleAiAnswer()  // Debe alternar el estado
```

---

## 📈 Mejoras Futuras

### 1. Persistir estado del toggle

Guardar en `localStorage` si el usuario cerró manualmente el toggle:

```javascript
localStorage.setItem('aiToggleClosed_' + preguntaId, 'true');
```

### 2. Animación más sofisticada

Usar `@keyframes` para entrada suave del toggle:

```css
@keyframes slideIn {
    from { opacity: 0; transform: translateY(-10px); }
    to { opacity: 1; transform: translateY(0); }
}

.ai-answer-toggle-wrapper {
    animation: slideIn 0.5s ease-out;
}
```

### 3. WebSockets en lugar de polling

Implementar SignalR para notificación en tiempo real cuando la respuesta está lista:

```csharp
await Clients.Group($"pregunta_{preguntaId}").SendAsync("AiAnswerReady", respuestaId);
```

### 4. Preview de la respuesta

Mostrar los primeros 100 caracteres en el header cuando está cerrado:

```html
<div class="ai-toggle-header">
    <span>🤖 Respuesta Informativa (IA)</span>
    <span class="ai-preview">Gracias por tu pregunta. En general...</span>
</div>
```

### 5. Feedback del usuario

Agregar botones "Útil" / "No útil" específicos para la respuesta de IA:

```html
<div class="ai-feedback">
    <button>👍 Útil</button>
    <button>👎 No útil</button>
</div>
```

---

## 📝 Checklist de Verificación

Antes de marcar como completado, verificar:

- [ ] Toggle se muestra correctamente cuando hay respuesta de IA
- [ ] Toggle abre/cierra al hacer clic
- [ ] Estado inicial correcto (abierto si <3 respuestas, cerrado si ≥3)
- [ ] Indicador "Procesando..." aparece en preguntas nuevas
- [ ] Polling inicia automáticamente
- [ ] Polling detecta cuando la respuesta está lista
- [ ] Página recarga automáticamente al detectar respuesta
- [ ] Respuesta de IA NO aparece en la lista normal de respuestas
- [ ] Separador visual se muestra correctamente
- [ ] Respuesta de IA soporta votos (si usuario autenticado)
- [ ] Logs detallados en consola del navegador
- [ ] Logs detallados en output de Visual Studio

---

## 🎬 Demo Flow

### Video de Flujo Esperado:

1. **[00:00]** Usuario crea pregunta → Submit
2. **[00:01]** Página recarga → Muestra indicador "Procesando tu pregunta..." con spinner
3. **[00:05]** Polling inicia → Console logs: "🔄 [AI Polling] Intento 1/60"
4. **[00:20]** IA genera respuesta (backend logs: "🎉 [AI Job] COMPLETADO")
5. **[00:25]** Polling detecta → Console logs: "✅ Respuesta de IA detectada"
6. **[00:26]** Página recarga → Indicador desaparece, toggle aparece ABIERTO
7. **[00:30]** Usuario puede cerrar el toggle manualmente
8. **[00:35]** Usuario puede volver a abrir el toggle

---

**Fecha de implementación:** Enero 2025  
**Status:** ✅ Completado  
**Testing:** ⏳ Pendiente
