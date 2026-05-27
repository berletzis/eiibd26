# Sesión: NINA Pipeline + Termino/Detalle mejoras
**Fecha:** 2026
**Rama:** master
**Archivos tocados:** 8 código + 1 tabla SQL

---

## 1. AIRequestLog — conectar al pipeline de IA

### Problema
El modelo `AIRequestLog.cs` existía con todos sus campos, pero:
- No estaba en `ApplicationDbContext` (sin `DbSet`)
- No existía la tabla en la BD de producción
- `AiAnswerJob` nunca lo usaba

Resultado: cero registros de métricas, sin trazabilidad de uso o errores.

### Solución

**SQL directo (producción):**
```sql
CREATE TABLE [dbo].[AIRequestLog] (
	[Id]               UNIQUEIDENTIFIER NOT NULL DEFAULT NEWSEQUENTIALID(),
	[PreguntaId]       UNIQUEIDENTIFIER NOT NULL,
	[QuestionText]     NVARCHAR(MAX)    NOT NULL DEFAULT '',
	[Level]            INT              NOT NULL DEFAULT 0,
	[HighRisk]         BIT              NOT NULL DEFAULT 0,
	[ModelUsed]        NVARCHAR(200)    NOT NULL DEFAULT '',
	[ProcessingTimeMs] FLOAT            NOT NULL DEFAULT 0,
	[Timestamp]        DATETIMEOFFSET   NOT NULL DEFAULT SYSDATETIMEOFFSET(),
	[Success]          BIT              NOT NULL DEFAULT 0,
	[ErrorMessage]     NVARCHAR(MAX)    NULL,
	CONSTRAINT [PK_AIRequestLog] PRIMARY KEY ([Id]),
	CONSTRAINT [FK_AIRequestLog_Preguntas_PreguntaId]
		FOREIGN KEY ([PreguntaId]) REFERENCES [dbo].[Preguntas]([Id]) ON DELETE CASCADE
);
CREATE INDEX [IX_AIRequestLog_PreguntaId] ON [dbo].[AIRequestLog] ([PreguntaId]);
CREATE INDEX [IX_AIRequestLog_Timestamp]  ON [dbo].[AIRequestLog] ([Timestamp] DESC);
```

**`ApplicationDbContext.cs`:**
```csharp
// AI Request Logs
public DbSet<AIRequestLog> AIRequestLogs { get; set; }
```

**`AiAnswerJob.cs`** — log al inicio, persistido en éxito y en fallo:
- Éxito: `Success=true`, `ModelUsed`, `QuestionText`, `ProcessingTimeMs` — se guarda en el mismo `SaveChangesAsync`
- Fallo: `Success=false`, `ErrorMessage` — se guarda en bloque catch con `CancellationToken.None`

### Estado verificado en producción
```
✅ Tabla AIRequestLog creada
✅ 3 respuestas IA previas en Respuestas (EsIA=1)
✅ SystemUser NINA existe: nina@eiibd.com
```

---

## 2. Orden de NINA en bloque "Relación con EII"

### Regla implementada
- `totalHuman < 3` → NINA renderiza **antes** de los comentarios humanos
- `totalHuman >= 3` → NINA baja al **final** con separador punteado

`totalHuman` = suma de `humanComments.Count + MeaningComments.Count` del nivel de relación.

**Archivo:** `Pages/Glosario/Termino.cshtml`, dentro del `@foreach (var level in allLevels)`.

---

## 3. Bloque "Compartir Término" en sidebar

**Posición:** Después del div cierre de "Calificar término", antes del div "Aviso importante".

**URL:** `$"{req.Scheme}://{req.Host}/Termino/{Model.Term.Slug}"`

**Nombre del script JS:** `openSharePopup` (diferente a `openPopup` de Contenidos para evitar conflicto).

**Botones:** WhatsApp, Facebook, X, Email — todos con popup centrado 600×600.

---

## 4. Avatar de médicos en "Validado por Profesionales de la Salud"

### Cambios en cadena (3 archivos)

#### `GlossaryValidationCountsDto.cs`
```csharp
public class ValidationCommentDto
{
	public string UserDisplay { get; set; } = "";
	public string? AvatarUrl { get; set; }   // ← NUEVO
	// ...
}
```

#### `GlossaryService.cs` — en `GetValidationCountsAsync`
```csharp
// Obtener avatares del Perfil por UserId
var avatarDict = await _db.Perfil
	.AsNoTracking()
	.Where(p => userGuidList.Contains(p.idUser))
	.Select(p => new { p.idUser, p.Avatar })
	.ToListAsync();
var avatarByUser = avatarDict.ToDictionary(p => p.idUser, p => p.Avatar);

// Al crear el DTO:
avatarByUser.TryGetValue(guid, out var avatarVal);
string? avatarUrl = null;
if (!string.IsNullOrWhiteSpace(avatarVal) && avatarVal != "default.jpg")
{
	avatarUrl = avatarVal.StartsWith("/") ? avatarVal : "/" + avatarVal;
}
```

#### `Termino.cshtml` — reemplazar ícono por img
```html
<!-- Tipo 2 (comentarios con usuario): -->
<div class="user-avatar" style="width:36px;height:36px;flex:0 0 36px;">
	<img src="@(val.AvatarUrl ?? "/img/default-avatar.png")" alt="@val.UserDisplay"
		 loading="lazy" decoding="async"
		 onerror="this.onerror=null;this.src='/img/default-avatar.png';" />
</div>

<!-- Tipo 1 (descripción, sin usuario identificado): -->
<div class="user-avatar" style="width:36px;height:36px;flex:0 0 36px;">
	<img src="/img/default-avatar.png" alt="Médico" loading="lazy" decoding="async"
		 onerror="this.onerror=null;this.src='/img/default-avatar.png';" />
</div>
```

### Formato del campo `Perfil.Avatar` en BD
| Valor | Significado | AvatarUrl resultante |
|-------|-------------|----------------------|
| `null` | Sin foto | `null` → usa default |
| `""` | Sin foto | `null` → usa default |
| `"default.jpg"` | Sin foto real | `null` → usa default |
| `"/uploads/avatars/{guid}/avatar-N.png"` | Foto real | se usa tal cual |

---

## Decisiones de diseño

- **MeaningComments sin AvatarUrl:** Los comentarios de tipo 1 son `List<string>`, no tienen usuario identificado en el DTO actual. Se muestra el avatar default. Cambiarlos a `List<ValidationCommentDto>` es trabajo mayor — aplazado.
- **`openSharePopup` vs `openPopup`:** Se eligió nombre diferente al script de Contenidos para evitar conflictos en páginas donde ambos scripts pudieran estar presentes.
- **`AIRequestLog.Level`:** Se setea como `QuestionLevel.Simple` por defecto en el job, ya que la evaluación real de nivel la hace `NinaModelRouterService` que no está integrado directamente en `AiAnswerJob`. Pendiente integrar si se agrega el router.
