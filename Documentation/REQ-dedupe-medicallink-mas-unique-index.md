# REQ — Depurar MedicalLink duplicados + índice UNIQUE (glosario: definición determinista)

**Tipo:** limpieza de **datos** (borra 24 filas de `GlossaryTermMedicalLink`) + **esquema** (índice único). SQL-directo, sin migración EF.
**Scope de datos:** SOLO `GlossaryTermMedicalLink`. **NO** tocar `tratamientos`, `sintomas`, ni sus registros vacíos (limpieza aparte). NO tocar NINA/Conectar3eros.
**Ejecución (AUTORIZADO por Berletzis):** corre los 4 pasos directo, **sin pedir confirmación de nada** — pre-check, borrado, post-check e índice UNIQUE, y muestra los resultados al final. Berletzis ya dio el visto bueno para borrar estos 24 links en prod; no lo vuelvas a solicitar. (Riesgo bajo: se borran links a stubs vacíos, sin usuarios; el link que queda es el que tiene la descripción real.)
**ÚNICA condición de parada que sí debes respetar:** el gate del Paso 1. Si el pre-check **no** devuelve exactamente los 24 LinkId listados, **detente y avisa** en vez de borrar — significa que los datos cambiaron. Fuera de ese caso, no preguntes nada y ejecuta hasta el final.

## Diagnóstico (ya corrido, BD 64.202.187.218)
24 términos con 2 links c/u; 0 huérfanos. En los 24, patrón idéntico: un registro **con descripción** (keeper) + un **stub vacío** (sin `DescripcionIA`, `RelacionEII=0`, `ValidadoHumano=1`, nombre en variante de caso/espacios), 0 usuarios en ambos. Sin casos divergentes → cero decisión clínica. La regla de ranking eligió el correcto en los 24 (incluye Temblor → síntoma 57).

## Los 24 links a BORRAR (RankKeep=2), para verificar el set exacto
`257, 303, 464, 998, 1016, 1261, 1849, 3215, 3435, 3507, 4793, 5119, 5792, 6078, 6292, 6517, 7197, 7861, 9075, 188, 9329, 9879, 9940, 10056`
El keeper de cada término es el otro link del par (RankKeep=1, el que trae `DescripcionIA`).

## Paso 1 — Pre-check (confirmar el set antes de borrar)
```sql
;WITH ranked AS (
    SELECT l.Id AS LinkId, l.GlossaryTermId,
           ROW_NUMBER() OVER (
               PARTITION BY l.GlossaryTermId
               ORDER BY
                   COALESCE(t.Eliminado, s.Eliminado, 0) ASC,
                   CASE WHEN NULLIF(LTRIM(RTRIM(COALESCE(t.DescripcionIA, s.DescripcionIA))),'') IS NULL THEN 0 ELSE 1 END DESC,
                   COALESCE(t.RelacionEII, s.RelacionEII, 0) DESC,
                   COALESCE(t.ValidadoHumano, s.ValidadoHumano, 0) DESC,
                   l.Id ASC
           ) AS rn
    FROM GlossaryTermMedicalLink l
    LEFT JOIN tratamientos t ON t.id = l.TratamientoId
    LEFT JOIN sintomas   s ON s.id = l.SintomaId
    WHERE l.GlossaryTermId IN (
        SELECT GlossaryTermId FROM GlossaryTermMedicalLink GROUP BY GlossaryTermId HAVING COUNT(*) > 1)
)
SELECT LinkId FROM ranked WHERE rn > 1 ORDER BY LinkId;
```
**Gate de seguridad:** este SELECT debe devolver **exactamente los 24 LinkId** de arriba. Si difiere (otro número, otro Id), **detenerse y avisar** — algo cambió en los datos.

## Paso 2 — Borrado (idempotente, mismo criterio)
```sql
;WITH ranked AS ( /* … idéntico al Paso 1 … */ )
DELETE FROM GlossaryTermMedicalLink WHERE Id IN (SELECT LinkId FROM ranked WHERE rn > 1);
```
Regla: por término, se conserva `rn = 1` (el de descripción / no eliminado) y se borra el resto. Re-correrlo no borra nada (ya no hay duplicados).

## Paso 3 — Post-check
```sql
-- 0 términos con duplicado
SELECT COUNT(*) FROM (SELECT GlossaryTermId FROM GlossaryTermMedicalLink
                      GROUP BY GlossaryTermId HAVING COUNT(*) > 1) x;   -- esperado 0
-- El link que sobrevive por término trae descripción (spot-check Temblor → SintomaId 57)
SELECT l.GlossaryTermId, l.Id AS LinkId, l.SintomaId, l.TratamientoId
FROM GlossaryTermMedicalLink l WHERE l.GlossaryTermId IN (56, 3496, 7843) ORDER BY l.GlossaryTermId;
```

## Paso 4 — Índice UNIQUE (prevenir recurrencia)
Solo después de que el post-check dé 0 duplicados (si no, falla):
```sql
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_GlossaryTermMedicalLink_Term'
              AND object_id = OBJECT_ID('dbo.GlossaryTermMedicalLink'))
    CREATE UNIQUE INDEX UQ_GlossaryTermMedicalLink_Term
        ON dbo.GlossaryTermMedicalLink (GlossaryTermId);
```
(Opcional, sin migración: alinear la config EF a 1:1 explícita — `HasOne(gt => gt.MedicalLink).WithOne(...).HasForeignKey<GlossaryTermMedicalLink>(x => x.GlossaryTermId)` — para que el modelo declare lo que ahora garantiza el índice.)

## Opcional — lectura determinista (red de seguridad)
Con el índice UNIQUE ya no puede haber duplicados, así que `gt.MedicalLink` en `GlossaryService.GetTermBySlugAsync` (L131–132) queda garantizado a una fila. No es necesario cambiar la consulta. (Si se quisiera blindar por si el índice no estuviera, se resolvería el link con un `OrderBy(x => x.Id)` explícito — pero requiere autorización por ser cambio de consulta, y es redundante con el índice.)

## No romper / cuidado
- **No** borrar ni modificar los registros vacíos en `tratamientos`/`sintomas` (p. ej. tratamiento 9597 "ABRAZOS"): pueden estar referenciados en otros lados; su limpieza es un tema aparte, con su propio análisis.
- Efecto colateral **positivo**: tras el borrado, esos 24 términos dejan de mostrar "Definición pendiente de generar" y rinden su definición real (aplica en prod de inmediato, no depende del deploy de vistas).

## Verificación funcional
1. Post-check: 0 duplicados; Temblor conserva SintomaId 57.
2. Abrir 2–3 de esos términos en el sitio → muestran la definición real (no "pendiente de generar").
3. Índice `UQ_GlossaryTermMedicalLink_Term` presente y único.
