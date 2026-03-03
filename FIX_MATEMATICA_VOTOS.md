# ✅ FIX: Matemática de Votos en Preguntas y Respuestas (Opción B: Flexible)

## 🐛 Problemas Identificados

### Problema 1: Toggle Restrictivo en Backend ❌

**Ubicación:** `PreguntasApiController.cs` y `RespuestasApiController.cs`

**Código Anterior:**
```csharp
if (existing.Valor == dto.Valor)
{
    if (!existing.Eliminado)
    {
        existing.Eliminado = true;  // Cancela
    }
    else
    {
        // NO hace nada → NO permite re-votar ❌
    }
}
```

**Bug:** Después de cancelar un voto, el usuario **NO podía volver a votar**. Esto es muy restrictivo y poco user-friendly.

---

### Problema 2: Frontend Bloquea Re-votación ❌

**Ubicación:** `Preguntas.cshtml` y `Detalles.cshtml`

**Código Anterior:**
```csharp
var disableUp = Model.Pregunta.UsuarioVoto != 0;  // ❌ Deshabilita después de votar
```

```javascript
if (currentUserVote !== 0) {
    return;  // ❌ Bloquea cualquier voto si ya votó
}
```

**Bug:** Una vez que el usuario votaba, los botones se deshabilitaban permanentemente.

---

### Problema 3: Frontend Calcula Score con Fallback Incorrecto ⚠️

**Ubicación:** `Preguntas.cshtml` (líneas ~575-576)

**Código Anterior:**
```javascript
const newScore = (j.score !== undefined) 
    ? parseInt(j.score, 10) 
    : (currentScore + (dir === 'up' ? 1 : -1));  // ❌ Fallback suma/resta siempre
```

**Bug:** El fallback no consideraba que el voto podía cancelarse.

---

## ✅ Soluciones Implementadas (Opción B: Flexible)

### 1. Backend: Permitir Toggle Flexible

**Archivos:** 
- `eiibd26/Controllers/PreguntasApiController.cs` (líneas ~216-234)
- `eiibd26/Controllers/RespuestasApiController.cs` (líneas ~223-241)

**Código Corregido:**
```csharp
else
{
    // Usuario ya votó previamente
    if (existing.Valor == votoDto.Valor)
    {
        // Intenta votar lo mismo (ej: +1 cuando ya votó +1)
        // Toggle: activo ↔ cancelado (permite re-votación flexible)
        existing.Eliminado = !existing.Eliminado;  // ✅ Toggle infinito permitido
        if (hasFechaModificacion) existing.FechaModificacion = DateTimeOffset.UtcNow;
        _db.Votos.Update(existing);
    }
    else
    {
        // Intenta cambiar el voto (ej: -1 cuando ya votó +1)
        // Eliminar el voto anterior para permitir crear uno nuevo
        existing.Eliminado = true;
        if (hasFechaModificacion) existing.FechaModificacion = DateTimeOffset.UtcNow;
        _db.Votos.Update(existing);
    }
}
```

**Comportamiento Nuevo:**
- Click 1 (upvote): Crea voto +1 (`Eliminado = false`) → Score +1
- Click 2 (upvote): Cancela voto (`Eliminado = true`) → Score -1
- **Click 3 (upvote): Reactiva voto (`Eliminado = false`)** → Score +1 ✅ **PERMITE RE-VOTAR**
- Click 4+: Toggle continúa...

**Resultado:** Usuario puede votar → cancelar → votar → cancelar infinitamente.

---

### 2. Frontend: Habilitar Botones Siempre (Preguntas)

**Archivos:**
- `eiibd26/Pages/Preguntas.cshtml` (líneas ~225, 549-554, 586-599)
- `eiibd26/Pages/Preguntas/Detalles.cshtml` (líneas ~252, 407, 504)

**Cambios en el Razor (servidor):**
```csharp
// ANTES:
var disableUp = q.UsuarioVoto != 0;  // ❌ Deshabilita si ya votó

// DESPUÉS:
var disableUp = false;  // ✅ Siempre habilitado (permite toggle)
```

**Cambios en JavaScript:**
```javascript
// ANTES:
if (currentUserVote !== 0) {
    return;  // ❌ Bloquea si ya votó
}

// DESPUÉS:
// (Código eliminado) ✅ Permite votar siempre
// Solo bloquea downvote si score <= 0
if (dir === 'down' && currentScore <= 0) {
    return;
}
```

**Cambios en actualización de botones:**
```javascript
// ANTES:
if (upBtn) {
    upBtn.disabled = newUserVote !== 0;  // ❌ Deshabilita después de votar
}

// DESPUÉS:
if (upBtn) {
    upBtn.disabled = false;  // ✅ Siempre habilitado
}
```

---

### 3. Frontend: Mejorar Score del Servidor

**Archivo:** `eiibd26/Pages/Preguntas.cshtml` (líneas ~573-584)

**Código Corregido:**
```javascript
const j = await res.json();
const scoreEl = document.getElementById('score-' + id);

// Usar score del servidor (siempre confiable)
const newScore = (j.score !== undefined) ? parseInt(j.score, 10) : currentScore;
const newUserVote = (j.userVote !== undefined) ? parseInt(j.userVote, 10) : 0;

if (scoreEl) scoreEl.textContent = String(newScore);
article.setAttribute('data-score', String(newScore));
article.setAttribute('data-user-vote', String(newUserVote));
```

**Mejora:** Fallback seguro que NO calcula incorrectamente.

---

## 📊 Fórmula de Score (Sin Cambios)

```sql
-- Backend (correcto):
Score = SUM(Votos.Valor WHERE EntidadTipo = 'pregunta' AND EntidadId = X AND Eliminado = false)
```

**Ejemplo:**
- Usuario A vota +1 → Score = +1
- Usuario B vota +1 → Score = +2
- Usuario C vota -1 → Score = +1
- Usuario A cancela su voto → Score = 0 (su voto marcado `Eliminado = true`)

---

## 🎯 Flujo de Votación Corregido (Opción B: Flexible)

### Escenario 1: Usuario vota Upvote

| Acción | Backend | Score | userVote | Estado DB |
|--------|---------|-------|----------|-----------|
| Sin voto | - | 0 | 0 | No existe registro |
| Click 1 (up) | Crea voto +1 | 1 | 1 | `Valor=1, Eliminado=false` |
| Click 2 (up) | Cancela voto (toggle) | 0 | 0 | `Valor=1, Eliminado=true` ✅ |
| Click 3 (up) | **Reactiva voto (toggle)** | 1 | 1 | `Valor=1, Eliminado=false` ✅ |
| Click 4 (up) | Cancela voto (toggle) | 0 | 0 | `Valor=1, Eliminado=true` ✅ |

**Resultado:** Permite toggle infinito (votar → cancelar → votar...) ✅

---

### Escenario 2: Usuario cambia de Upvote a Downvote

| Acción | Backend | Score | userVote | Estado DB |
|--------|---------|-------|----------|-----------|
| Sin voto | - | 0 | 0 | No existe registro |
| Click upvote | Crea voto +1 | 1 | 1 | `Valor=1, Eliminado=false` |
| Click downvote | Elimina voto +1 | 0 | 0 | `Valor=1, Eliminado=true` ✅ |
| *Segunda petición* | *Crea voto -1* | -1 | -1 | `Valor=-1, Eliminado=false` |

**Nota:** El cambio de voto requiere **DOS clicks** (cancelar, luego votar opuesto). Esto previene cambios accidentales.

---

## 🧪 Casos de Prueba

### Caso 1: Votación Simple

**Pasos:**
1. Usuario A hace login
2. Vota upvote en pregunta ID=123
3. Verifica que score = 1 y botón upvote está deshabilitado
4. Hace clic en upvote nuevamente
5. Verifica que score = 0 y botón upvote está habilitado

**SQL Verificación:**
```sql
SELECT * FROM Votos 
WHERE EntidadTipo = 'pregunta' 
  AND EntidadId = '123' 
  AND UsuarioId = 'GUID-del-usuario';
-- Debe mostrar 1 registro con Eliminado = true
```

---

### Caso 2: Permitir Re-Votación

**Pasos:**
1. Usuario A vota upvote (Score = 1)
2. Cancela upvote (Score = 0)
3. Hace clic en upvote de nuevo
4. **Esperado:** Score = 1 ✅ **PERMITE re-votar (Opción B)**

**SQL Verificación:**
```sql
SELECT Valor, Eliminado, FechaModificacion
FROM Votos 
WHERE EntidadTipo = 'pregunta' 
  AND EntidadId = '123' 
  AND UsuarioId = 'GUID-del-usuario';
-- Debe mostrar: Valor=1, Eliminado=false, FechaModificacion actualizada
```

---

### Caso 3: Múltiples Usuarios

**Pasos:**
1. Usuario A vota upvote → Score = 1
2. Usuario B vota upvote → Score = 2
3. Usuario C vota downvote → Score = 1
4. Usuario A cancela su voto → Score = 0
5. Usuario B cancela su voto → Score = -1 (solo queda el downvote de C)

**SQL Verificación:**
```sql
SELECT UsuarioId, Valor, Eliminado
FROM Votos 
WHERE EntidadTipo = 'pregunta' 
  AND EntidadId = '123'
ORDER BY FechaCreacion;
```

**Resultado Esperado:**
```
UsuarioId       Valor  Eliminado
UserA-GUID      1      true      (cancelado)
UserB-GUID      1      true      (cancelado)
UserC-GUID      -1     false     (activo)
```

---

## 🚀 Archivos Modificados

| Archivo | Líneas | Cambio |
|---------|--------|--------|
| `PreguntasApiController.cs` | 216-234 | ✅ Toggle flexible (permite re-votación) |
| `RespuestasApiController.cs` | 223-241 | ✅ Toggle flexible (permite re-votación) |
| `Preguntas.cshtml` | 225 | ✅ `disableUp = false` |
| `Preguntas.cshtml` | 549-554 | ✅ Remover bloqueo por userVote |
| `Preguntas.cshtml` | 586-599 | ✅ Botones siempre habilitados |
| `Detalles.cshtml` | 252, 407, 504 | ✅ `disableUp = false` |

**Total:** 4 archivos modificados

---

## 📝 Próximos Pasos

### 1. **Reiniciar la Aplicación:**
```
Shift+F5 (detener)
F5 (iniciar)
```

### 2. **Probar Votación (Opción B - Flexible):**

**En `/Preguntas`:**
1. Login como Usuario A
2. Vota upvote en una pregunta → Score aumenta ✅
3. Click upvote nuevamente → Score disminuye (cancela) ✅
4. Click upvote de nuevo → **Score aumenta (re-vota)** ✅
5. Verifica que puedes hacer toggle infinitamente ✅

**En `/Preguntas/Detalles/{slug}`:**
1. Probar votación en pregunta principal
2. Probar votación en respuesta aceptada
3. Probar votación en otras respuestas
4. Verificar que el toggle funciona en todos los casos ✅

### 3. **Verificar en SQL:**
```sql
-- Ver votos de un usuario en preguntas
SELECT 
    p.Titulo,
    v.Valor,
    v.Eliminado,
    v.FechaCreacion,
    v.FechaModificacion
FROM Votos v
INNER JOIN Preguntas p ON v.EntidadId = p.Id
WHERE v.UsuarioId = 'GUID-del-usuario'
  AND v.EntidadTipo = 'pregunta'
ORDER BY v.FechaCreacion DESC;
```

---

## ⚠️ Características del Sistema (Opción B: Flexible)

1. **Toggle Infinito Permitido:**
   - Puedes votar → cancelar → votar → cancelar... infinitamente ✅
   - Esto es similar a Stack Overflow, Reddit, etc.

2. **Cambio de Voto Requiere 2 Clicks:**
   - Click 1: Cancela voto actual
   - Click 2: Vota opuesto
   - **Esto previene cambios accidentales**

3. **Frontend Usa Score del Servidor:**
   - Si el API devuelve `score`, lo usa
   - Si falla, mantiene `currentScore` (no calcula)
   - **Más seguro y confiable**

4. **Downvote Bloqueado en Score 0:**
   - No se puede hacer downvote si el score ya es 0 o negativo
   - **Previene scores negativos extremos**

---

## 📊 Comparativa Antes/Después

### ANTES del Fix:

| Acción | Score | Problema |
|--------|-------|----------|
| Click 1 (up) | 1 | ✅ Correcto |
| Click 2 (up) | 0 | ✅ Cancela (correcto) |
| Click 3 (up) | **0** | ❌ **NO permite re-votar (muy restrictivo)** |

**Resultado:** Usuario bloqueado después de cancelar.

### DESPUÉS del Fix (Opción B):

| Acción | Score | Resultado |
|--------|-------|-----------|
| Click 1 (up) | 1 | ✅ Vota |
| Click 2 (up) | 0 | ✅ Cancela |
| Click 3 (up) | **1** | ✅ **RE-VOTA (flexible)** |
| Click 4 (up) | **0** | ✅ **Cancela de nuevo** |
| Click 5+ (up) | **Toggle...** | ✅ **Continúa permitiendo toggle** |

**Resultado:** Usuario puede cambiar de opinión libremente ✅

---

## ✅ Estado Final

| Aspecto | Estado |
|---------|--------|
| 🟢 Backend (Preguntas) | ✅ Corregido |
| 🟢 Backend (Respuestas) | ✅ Corregido |
| 🟢 Frontend (Index) | ✅ Mejorado |
| 🟢 Build | ✅ Exitoso |
| 🟡 Testing | ⏳ Pendiente verificación |

---

**Estado:** ✅ Implementado y listo para pruebas

**Prioridad:** 🔴 Alta (afecta integridad de votos)

**Riesgo:** 🟢 Bajo (solo mejora la lógica existente)
