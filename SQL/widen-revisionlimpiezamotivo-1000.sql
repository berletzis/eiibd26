/*
    widen-revisionlimpiezamotivo-1000.sql
    ---------------------------------------------------------------------------
    Ensancha RevisionLimpiezaMotivo de NVARCHAR(400) a NVARCHAR(1000) en
    sintomas y tratamientos. La justificación del triage (por qué Dudoso/Basura)
    se conserva completa; antes se truncaba a 400 y se perdía cola de razonamiento.

    Idempotente (revisa el tamaño actual antes de alterar). Sin migración EF Core
    (política del proyecto). NVARCHAR nullable → ALTER COLUMN es online y no toca
    datos existentes.
*/

SET NOCOUNT ON;

-- ── sintomas ────────────────────────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.sintomas')
             AND name = 'RevisionLimpiezaMotivo'
             AND max_length <> 2000)   -- NVARCHAR(1000) = 2000 bytes
BEGIN
    ALTER TABLE dbo.sintomas ALTER COLUMN RevisionLimpiezaMotivo NVARCHAR(1000) NULL;
    PRINT 'ALTER dbo.sintomas.RevisionLimpiezaMotivo -> NVARCHAR(1000)';
END
ELSE PRINT 'SKIP dbo.sintomas.RevisionLimpiezaMotivo (ya es 1000)';

-- ── tratamientos ─────────────────────────────────────────────────────────────
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.tratamientos')
             AND name = 'RevisionLimpiezaMotivo'
             AND max_length <> 2000)
BEGIN
    ALTER TABLE dbo.tratamientos ALTER COLUMN RevisionLimpiezaMotivo NVARCHAR(1000) NULL;
    PRINT 'ALTER dbo.tratamientos.RevisionLimpiezaMotivo -> NVARCHAR(1000)';
END
ELSE PRINT 'SKIP dbo.tratamientos.RevisionLimpiezaMotivo (ya es 1000)';
GO

-- Verificación
SELECT t.name AS tabla, c.name AS columna,
       TYPE_NAME(c.user_type_id) AS tipo, c.max_length AS bytes, c.is_nullable
FROM sys.columns c
JOIN sys.tables  t ON t.object_id = c.object_id
WHERE c.name = 'RevisionLimpiezaMotivo'
  AND t.name IN ('sintomas', 'tratamientos')
ORDER BY t.name;
