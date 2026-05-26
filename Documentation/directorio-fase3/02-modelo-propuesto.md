# Modelo Propuesto — Granularidad EII en Confirmaciones

**Fecha:** 2026-05-25

---

## Opciones evaluadas

### Opción A — FK nullable en `ConfirmacionComunitaria`

```
ConfirmacionComunitaria
├── ...
└── AreaExperienciaEiiId INT? NULL  ← nueva columna
```

**Ventajas:**
- Schema mínimo (una columna, no una tabla)
- Query simple (no join adicional)

**Desventajas:**
- **Un solo área por confirmación.** Un paciente puede haber sido atendido por Crohn Y biológicos — no puede expresarlo.
- Cambio destructivo al modelo si luego necesitamos múltiples áreas
- No refleja la realidad clínica (EII tiene comorbilidades frecuentes)

**Veredicto: DESCARTADA** — limita artificialmente la expresividad del dato.

---

### Opción B — Nueva tabla `ConfirmacionComunitariaArea` ✅ RECOMENDADA

```
ConfirmacionComunitaria (1)
    └── (N) ConfirmacionComunitariaArea
                ├── ConfirmacionComunitariaId  FK → ConfirmacionComunitaria.Id
                └── AreaExperienciaEiiId       FK → AreaExperienciaEii.Id
```

**Ventajas:**
- Un paciente puede confirmar múltiples áreas en una sola confirmación
- Reutiliza `AreaExperienciaEii` (taxonomía ya existe, sin crear nada nuevo)
- Aditivo: no modifica `ConfirmacionComunitaria` existente
- Escalable: agregar áreas nuevas no requiere schema change
- Datos históricos sin área siguen siendo válidos (la tabla nueva empieza vacía para confirmaciones antiguas)

**Desventajas:**
- Un join adicional en queries que necesiten áreas
- Requiere nueva tabla en BD (script SQL idempotente)

**Veredicto: RECOMENDADA.**

---

### Opción C — Reusar `TipoConfirmacion` para codificar áreas

Crear TipoConfirmacion records tipo "CUCI", "Crohn", etc.

**Descartada:** `TipoConfirmacion` captura el ROL del confirmador (paciente, familiar, profesional). Codificar áreas en la misma tabla mezcla dos dimensiones semánticas distintas. Rompería el significado de `TipoConfirmacion`.

---

### Opción D — Agregar áreas al médico vía `MedicoExperienciaEii` al confirmar

Al crear una confirmación, upsert en `MedicoExperienciaEii` en lugar de per-confirmación.

**Descartada para per-confirmación:** el Dashboard necesita saber QUÉ áreas confirmó CADA paciente (por fila). `MedicoExperienciaEii` es un agregado por médico, pierde la dimensión por confirmación.

> Nota: `MedicoExperienciaEii` puede actualizarse como cache de agregación al guardar `ConfirmacionComunitariaArea`, pero no reemplaza al modelo relacional.

---

## Diseño: Opción B

### Schema SQL

```sql
CREATE TABLE ConfirmacionComunitariaArea (
    Id                         INT IDENTITY(1,1) NOT NULL,
    ConfirmacionComunitariaId  INT NOT NULL,
    AreaExperienciaEiiId       INT NOT NULL,
    CONSTRAINT PK_ConfirmacionComunitariaArea PRIMARY KEY (Id),
    CONSTRAINT FK_CCA_Confirmacion FOREIGN KEY (ConfirmacionComunitariaId)
        REFERENCES ConfirmacionComunitaria(Id),
    CONSTRAINT FK_CCA_Area FOREIGN KEY (AreaExperienciaEiiId)
        REFERENCES AreaExperienciaEii(Id),
    CONSTRAINT UQ_CCA_Conf_Area UNIQUE (ConfirmacionComunitariaId, AreaExperienciaEiiId)
);

-- Índice para lookup rápido por médico (vía join con ConfirmacionComunitaria)
CREATE INDEX IX_CCA_ConfirmacionId ON ConfirmacionComunitariaArea(ConfirmacionComunitariaId);
```

Script idempotente (patrón CLAUDE.md):
```sql
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConfirmacionComunitariaArea')
BEGIN
    CREATE TABLE ConfirmacionComunitariaArea (
        Id                         INT IDENTITY(1,1) NOT NULL,
        ConfirmacionComunitariaId  INT NOT NULL,
        AreaExperienciaEiiId       INT NOT NULL,
        CONSTRAINT PK_ConfirmacionComunitariaArea PRIMARY KEY (Id),
        CONSTRAINT FK_CCA_Confirmacion FOREIGN KEY (ConfirmacionComunitariaId)
            REFERENCES ConfirmacionComunitaria(Id),
        CONSTRAINT FK_CCA_Area FOREIGN KEY (AreaExperienciaEiiId)
            REFERENCES AreaExperienciaEii(Id),
        CONSTRAINT UQ_CCA_Conf_Area UNIQUE (ConfirmacionComunitariaId, AreaExperienciaEiiId)
    );
    CREATE INDEX IX_CCA_ConfirmacionId ON ConfirmacionComunitariaArea(ConfirmacionComunitariaId);
END;
```

---

### Modelo C#

**Nuevo modelo** `Models/Directorio/ConfirmacionComunitariaArea.cs`:
```csharp
[Table("ConfirmacionComunitariaArea")]
public class ConfirmacionComunitariaArea
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ConfirmacionComunitariaId { get; set; }

    [Required]
    public int AreaExperienciaEiiId { get; set; }

    [ForeignKey(nameof(ConfirmacionComunitariaId))]
    public virtual ConfirmacionComunitaria Confirmacion { get; set; } = null!;

    [ForeignKey(nameof(AreaExperienciaEiiId))]
    public virtual AreaExperienciaEii Area { get; set; } = null!;
}
```

**Modificación** `Models/Directorio/ConfirmacionComunitaria.cs` — agregar navegación:
```csharp
// Agregar al final de la clase:
public virtual ICollection<ConfirmacionComunitariaArea> Areas { get; set; } = new List<ConfirmacionComunitariaArea>();
```

**`ApplicationDbContext`** — agregar DbSet:
```csharp
public DbSet<ConfirmacionComunitariaArea> ConfirmacionesComunitariasAreas { get; set; }
```

---

### Flujo de datos

```
CONFIRMACIÓN:
Usuario elige TipoConfirmacion (quién confirma)
       +
Usuario selecciona áreas EII del médico que experimentó
       ↓
POST OnPostConfirmarSimpleAsync
       ↓
INSERT ConfirmacionComunitaria (id generado)
       ↓
FOREACH area seleccionada:
    INSERT ConfirmacionComunitariaArea (ConfirmacionComunitariaId, AreaExperienciaEiiId)

LECTURA Dashboard:
_db.ConfirmacionesComunitarias
    .Include(c => c.Areas).ThenInclude(a => a.Area)
    .Where(...)
    .ToListAsync()
    
→ RecomendacionDashboardVm:
    ExpCUCI       = c.Areas.Any(a => a.Area.Nombre == "CUCI")
    ExpCrohn      = c.Areas.Any(a => a.Area.Nombre == "Crohn")
    ExpPediatrico = c.Areas.Any(a => a.Area.Nombre == "Pediátrico")
    ExpBiologicos = c.Areas.Any(a => a.Area.Nombre == "Biológicos")
    // (Vista no cambia — sigue leyendo los bool fields)

LECTURA Admin expContadores:
confs.Include(c => c.Areas).ThenInclude(a => a.Area)
→ expContadores = areas.Select(a => new { nombre = a, total = confs.Count(c => c.Areas.Any(ar => ar.Area.Nombre == a)) })
```

---

### Compatibilidad con datos históricos

- Confirmaciones existentes en `ConfirmacionComunitaria` no tendrán rows en `ConfirmacionComunitariaArea`.
- Al consultar áreas para confirmaciones históricas: `c.Areas` = lista vacía → `ExpCUCI = false` → comportamiento idéntico al actual (Fase 2).
- Sin datos corruptos ni migraciones de datos requeridas.

---

### Extensión futura opcional: `MedicoExperienciaEii` como cache

Después de insertar `ConfirmacionComunitariaArea`, se puede upsert en `MedicoExperienciaEii` para tener un índice de "este médico ha sido confirmado para CUCI por N pacientes". Esto evitaría joins costosos en el listado de tarjetas. Se documenta como extensión opcional, no requerida en Fase 3.
