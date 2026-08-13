# REQ — Quitar índice redundante IX_GlossaryTermMedicalLink_GlossaryTermId + alinear install scripts

**Tipo:** esquema (drop de índice no-único redundante) + alineación de scripts de instalación. SQL-directo, sin migración EF.
**Scope:** `GlossaryTermMedicalLink` (índice) + `SQL/Glossary/*` (scripts de install). NO tocar datos ni NINA/Conectar3eros.

## Contexto
Tras crear `UQ_GlossaryTermMedicalLink_Term` (UNIQUE sobre `GlossaryTermId`), el índice previo `IX_GlossaryTermMedicalLink_GlossaryTermId` (NO único, misma columna) quedó **redundante**: el UNIQUE sirve los mismos seeks. Mantenerlo solo agrega mantenimiento en cada escritura.

**Verificado (código):** ningún hint de consulta lo referencia por nombre (`WITH (INDEX...)` / `ForceIndex` → 0 resultados en la app). Referencias existentes:
- Migración `20260310160831_AddGlossaryValidation` + model snapshot de EF (lo declaran).
- Scripts de install `SQL/Glossary/00_Install_Glossary_Complete.sql`, `01_Create_GlossaryTables.sql`, `95_Install_Auto.sql`, `98_FORCE_Clean_Install.sql` (lo CREAN en install limpio, y crean el índice **no** único, no el UNIQUE).

## Riesgo
Bajo. Borrarlo no afecta lecturas (el UNIQUE cubre). El único cuidado: si no se tocan los install scripts, un install desde cero lo recrea (y sigue sin crear el UNIQUE). Por eso el REQ incluye alinear esos scripts.

## Cambio

### A. Prod — drop del índice redundante (SQL-directo, idempotente)
```sql
IF EXISTS (SELECT 1 FROM sys.indexes
           WHERE name = 'IX_GlossaryTermMedicalLink_GlossaryTermId'
             AND object_id = OBJECT_ID('dbo.GlossaryTermMedicalLink'))
    DROP INDEX IX_GlossaryTermMedicalLink_GlossaryTermId ON dbo.GlossaryTermMedicalLink;
```
Pre/post-check contra `sys.indexes`: antes están los dos índices sobre `GlossaryTermId` (el IX y el UQ); después solo `UQ_GlossaryTermMedicalLink_Term`.

### B. Install scripts — reflejar el 1:1
En `SQL/Glossary/00_Install_Glossary_Complete.sql`, `01_Create_GlossaryTables.sql`, `95_Install_Auto.sql`, `98_FORCE_Clean_Install.sql`: reemplazar la creación del índice **no** único por el **UNIQUE** (mismo nombre `UQ_GlossaryTermMedicalLink_Term`, `CREATE UNIQUE INDEX ... (GlossaryTermId)`), para que un install limpio nazca ya con la restricción correcta y sin el índice redundante.
- Nota: un install limpio parte de tabla vacía, así que el UNIQUE no puede fallar por datos.

### C. Modelo EF (opcional, sin migración)
El model snapshot aún declara el índice no único. El proyecto **no** corre migraciones, así que a runtime no afecta. Si se quiere evitar drift, alinear la config a `HasIndex(x => x.GlossaryTermId).IsUnique()` (o el `HasOne/WithOne` 1:1). **No** ejecutar `Add-Migration`/`Update-Database`.

## Ejecución (AUTORIZADO por Berletzis)
Aplica directo, **sin pedir confirmación**. La parte A (drop) es lo único que toca prod; B y C son archivos. Muestra pre/post-check al final. Único stop: si el pre-check no muestra el índice IX (ya no existe) o si aparece algo inesperado, avisar.

## Verificación
1. `sys.indexes` sobre `GlossaryTermMedicalLink`: queda `UQ_GlossaryTermMedicalLink_Term` (único), ya no está `IX_GlossaryTermMedicalLink_GlossaryTermId`.
2. Abrir un término del glosario → sigue resolviendo su definición (el UNIQUE sirve el seek).
3. Install scripts revisados: crean el UNIQUE, no el índice no único.
