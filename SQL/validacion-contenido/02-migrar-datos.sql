-- ============================================================
-- Migración: GlossaryValidation tipo=1 → ValidacionesContenidoProfesional
-- Ejecutar DESPUÉS de 01-crear-tabla.sql
-- Idempotente: no re-inserta registros ya migrados.
-- GlossaryValidation tipo=2 (RelationValidation) NO se toca.
-- ============================================================

DECLARE @Migrados INT = 0;

INSERT INTO dbo.ValidacionesContenidoProfesional (
    TipoContenido,
    ContenidoId,
    UsuarioMedicoId,
    Comentario,
    Estado,
    CreadoEn,
    ActualizadoEn,
    ModeradoPorId,
    FechaModeracion,
    NotaModeracion
)
SELECT
    1,              -- TipoContenido = Termino
    gv.GlossaryTermId,
    gv.UserId,
    gv.Comment,
    1,              -- Estado = Validado (Approved=true → Validado)
    gv.CreatedAt,
    NULL,           -- ActualizadoEn: no existía en tabla origen
    NULL,
    NULL,
    NULL
FROM dbo.GlossaryValidation gv
WHERE gv.ValidationType = 1   -- MeaningValidation únicamente
  AND NOT EXISTS (
    SELECT 1
    FROM dbo.ValidacionesContenidoProfesional vcp
    WHERE vcp.TipoContenido = 1
      AND vcp.ContenidoId = gv.GlossaryTermId
      AND vcp.UsuarioMedicoId = gv.UserId
  );

SET @Migrados = @@ROWCOUNT;
PRINT CONCAT('Registros migrados: ', @Migrados);

-- Verificación cruzada
SELECT
    'GlossaryValidation tipo=1 (origen)' AS Fuente,
    COUNT(*) AS Total
FROM dbo.GlossaryValidation
WHERE ValidationType = 1

UNION ALL

SELECT
    'ValidacionesContenidoProfesional tipo=Termino (destino)',
    COUNT(*)
FROM dbo.ValidacionesContenidoProfesional
WHERE TipoContenido = 1;

PRINT 'Script 02 completo. Verificar que los totales coinciden.';
