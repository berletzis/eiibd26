# REQ — Índice único de PlatNotaClinica: incluir TipoNota (desbloquea Precaución de seguridad)

> **Estado: IMPLEMENTADO** (commit `3e5bd7c`, corrido y verificado en prod). Script final: `SQL/alter-platnotaclinica-unique-tiponota.sql`. También se alineó el `HasIndex` fluent del `ApplicationDbContext` a la terna (sin migración).
> **Dos correcciones al SQL de este REQ (necesarias, aplicadas):**
> 1. **La tabla es `dbo.PlatNotaClinica` (singular)** — `PlatNotasClinicas` es el DbSet, no la tabla. Con el nombre equivocado, `OBJECT_ID` devuelve NULL → pre-check vacío, el `IF EXISTS` no entra y el `CREATE` revienta con "Invalid object name" (falla silenciosa hasta el final). Todos los `OBJECT_ID('dbo.PlatNotasClinicas')` de abajo deben leerse **`dbo.PlatNotaClinica`**.
> 2. **`UQ_PlatNotaClinica_Destino` es un UNIQUE CONSTRAINT, no un índice** (`is_unique_constraint = 1`). `DROP INDEX` falla sobre un constraint → se hizo con `ALTER TABLE ... DROP/ADD CONSTRAINT`. No disparó la condición de parada: no es PK, y las dos FKs (PlatNotaSeccion, PlatNotaReferencia) cuelgan de la PK clustered (`key_index_id = 1`), no de este objeto.
> **Verificación:** 0 ternas duplicadas en las 78 filas (las 3 columnas NOT NULL), post-check en terna, idempotente, e INSERT funcional de Precaución en grupo 26 (acelga, con Tolerancia existente) que antes reventaba. Datos temporales borrados, 0 residuos.


**Fecha:** 06 AGO 2026
**Tipo:** cambio de **esquema** (índice único). SQL-directo, **sin migraciones EF** (regla del proyecto).
**Scope de datos:** solo el índice `UQ_PlatNotaClinica_Destino` sobre `PlatNotasClinicas`. NO tocar filas, ni otras tablas, ni NINA/Conectar3eros.
**Ejecución (AUTORIZADO por Berletzis):** aplica directo, **sin pedir autorización ni confirmación de permisos** — corre el pre-check, ejecuta el SQL en prod (`64.202.187.218`), verifica con el post-check, y muestra los resultados al final. Berletzis ya dio el visto bueno para este cambio de esquema; no lo vuelvas a solicitar. El scope está acotado a este único índice. **Única condición de seguridad que sí debes respetar:** si el pre-check revela que `UQ_PlatNotaClinica_Destino` respalda una PK o FK (no es un índice único normal), **detente y avisa** en vez de forzar el `DROP` — ahí el cambio deja de ser trivial.

## Problema (verificado)
- `UQ_PlatNotaClinica_Destino` es `UNIQUE (TipoDestino, DestinoId)` — **sin `TipoNota`** en la clave y **sin filtro** (confirmado contra `sys.indexes`).
- Pero la app trata la nota como **una por (destino, tipo)**: `PlatNotaAdminService` busca/inserta por la terna `(TipoDestino, DestinoId, TipoNota)` en `GuardarBorradorAsync` (L101/L116), `PublicarAsync` (L171) y `DespublicarAsync` (L190). Es el **único escritor** de la tabla.
- Consecuencia: una **Precaución de seguridad es imposible de guardar en cualquier grupo/ingrediente que ya tenga una nota de Tolerancia** → el índice del par lo bloquea. Los **0 registros de Precaución en prod no son por falta de escritura, sino porque no se pueden guardar donde tienen sentido.** (Por eso la verificación del REQ anterior tuvo que sembrar en el grupo 36, que estaba libre.)

## Por qué es seguro
- El código **ya asume la terna**; no hay que cambiar la app. El índice es lo único desalineado.
- El índice nuevo `(TipoDestino, DestinoId, TipoNota)` es **menos restrictivo** que el actual `(TipoDestino, DestinoId)`: cualquier fila que hoy cumple el par, cumple la terna. **Recrearlo no puede fallar por datos duplicados.**
- No agrega ni quita columnas → **no hay riesgo "Invalid column name"**; no aplica la regla de orden ADD-antes/DROP-después. El SQL se puede correr de forma independiente y solo **afloja** la restricción.

## Cambio (SQL-directo, idempotente)
Recrear el índice único incluyendo `TipoNota`. Antes de correr, **verificar el nombre y las columnas reales** contra `sys.indexes`/`sys.index_columns` (por si el nombre difiere).

```sql
-- 1. Pre-check: confirmar definición actual
SELECT i.name, c.name AS Col, ic.key_ordinal, i.is_unique, i.has_filter, i.filter_definition
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID('dbo.PlatNotasClinicas') AND i.name = 'UQ_PlatNotaClinica_Destino'
ORDER BY ic.key_ordinal;

-- 2. Recrear como UNIQUE (TipoDestino, DestinoId, TipoNota)
IF EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'UQ_PlatNotaClinica_Destino' AND object_id = OBJECT_ID('dbo.PlatNotasClinicas'))
    DROP INDEX UQ_PlatNotaClinica_Destino ON dbo.PlatNotasClinicas;

CREATE UNIQUE INDEX UQ_PlatNotaClinica_Destino
    ON dbo.PlatNotasClinicas (TipoDestino, DestinoId, TipoNota);

-- 3. Post-check: confirmar que la terna quedó
SELECT c.name AS Col, ic.key_ordinal
FROM sys.indexes i
JOIN sys.index_columns ic ON ic.object_id = i.object_id AND ic.index_id = i.index_id
JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
WHERE i.object_id = OBJECT_ID('dbo.PlatNotasClinicas') AND i.name = 'UQ_PlatNotaClinica_Destino'
ORDER BY ic.key_ordinal;
```

> Nota: si el índice está respaldando una PK o una FK, `DROP INDEX` fallará y habría que tratarlo como constraint (`ALTER TABLE ... DROP/ADD CONSTRAINT`). El pre-check lo revela. Es un índice único normal según lo verificado, así que `DROP INDEX` debería bastar.

## Opcional (alinear el modelo, sin migración)
Si el `DbContext` declara `HasIndex(x => new { x.TipoDestino, x.DestinoId }).IsUnique()`, actualizar esa configuración fluent a la **terna** `{ TipoDestino, DestinoId, TipoNota }` para que el modelo no drifte respecto a la BD. **No** correr `Add-Migration`/`Update-Database` (regla del proyecto). Es solo para mantener el modelo honesto; el runtime funciona igual con o sin este ajuste.

## Verificación (no hipotética)
1. Correr el SQL en la BD del app (`64.202.187.218`); post-check muestra la terna.
2. Desde el admin de notas (o SQL temporal marcado TEMPORAL-CLAUDE), crear una **Precaución** en un grupo que **ya tenga** nota de Tolerancia → **guarda sin reventar** el UNIQUE. (Antes: fallaba.)
3. La ficha del ingrediente de ese grupo muestra las dos notas: Tolerancia (card blanco) + Precaución (card ámbar), cada una con su sello/leyenda según el REQ anterior.
4. Limpieza si se usó dato temporal: borrar por Id; confirmar 0 residuos por query; el conteo de notas vuelve al de partida.

## Coordinación de deploy
- `pushear ≠ desplegar`: el fix de vista de Precaución (commit `652ae76`) y este índice deben estar **ambos** en prod para que la Precaución sea usable de verdad. El SQL es seguro de correr en cualquier momento (solo afloja); el build de la vista sube cuando Berletzis despliegue.
