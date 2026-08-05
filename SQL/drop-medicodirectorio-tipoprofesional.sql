/* ============================================================================
   drop-medicodirectorio-tipoprofesional.sql
   ----------------------------------------------------------------------------
   Elimina dbo.MedicosDirectorio.TipoProfesional — la columna VIEJA, ya movida a
   dbo.MedicoPerfilExtendido.TipoProfesional (perfil por-usuario).

   ⚠️ ORDEN DE DESPLIEGUE — este script va AL FINAL:
       1) add-medicoperfilextendido-tipoprofesional.sql   (crea la nueva + backfill)
       2) desplegar el código del "move"
       3) ESTE script                                      (dropea la vieja)

   Correrlo ANTES del paso 2 no rompe nada (el código viejo ya no la lee después
   del deploy), pero correrlo antes del paso 1 PIERDE los datos: el backfill de
   ficha → perfil lee justamente esta columna. Mientras siga ahí es inofensiva:
   ningún modelo EF la declara ya, y EF solo mapea propiedades declaradas.

   Idempotente: se puede re-correr sin efecto.
   ============================================================================ */
SET NOCOUNT ON;
GO

/* ---------- 0) Guarda de seguridad ----------
   No dropear si la columna nueva no existe todavía: significaría que el paso 1
   no se corrió y estaríamos tirando el único lugar donde vive el dato. */
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.MedicoPerfilExtendido')
                 AND name = 'TipoProfesional')
BEGIN
    RAISERROR('ABORTADO: MedicoPerfilExtendido.TipoProfesional no existe. Corre primero add-medicoperfilextendido-tipoprofesional.sql.', 16, 1);
END
GO

/* ---------- 1) CHECK constraint (va antes que la columna) ---------- */
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.MedicoPerfilExtendido') AND name = 'TipoProfesional')
   AND EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = 'CK_MedicosDirectorio_TipoProfesional'
                 AND parent_object_id = OBJECT_ID('dbo.MedicosDirectorio'))
BEGIN
    ALTER TABLE dbo.MedicosDirectorio DROP CONSTRAINT CK_MedicosDirectorio_TipoProfesional;
    PRINT 'CK_MedicosDirectorio_TipoProfesional eliminada.';
END
ELSE
    PRINT 'CK_MedicosDirectorio_TipoProfesional no existía — sin cambios.';
GO

/* ---------- 2) Columna ---------- */
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('dbo.MedicoPerfilExtendido') AND name = 'TipoProfesional')
   AND EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.MedicosDirectorio') AND name = 'TipoProfesional')
BEGIN
    ALTER TABLE dbo.MedicosDirectorio DROP COLUMN TipoProfesional;
    PRINT 'MedicosDirectorio.TipoProfesional eliminada.';
END
ELSE
    PRINT 'MedicosDirectorio.TipoProfesional no existía — sin cambios.';
GO

/* ---------- Verificación ---------- */
-- Debe devolver 0 filas.
SELECT c.name AS ColumnaVieja
FROM sys.columns c
WHERE c.object_id = OBJECT_ID('dbo.MedicosDirectorio')
  AND c.name = 'TipoProfesional';

-- Debe devolver 1 fila: la nueva, viva.
SELECT c.name AS ColumnaNueva, t.name AS Tipo, c.is_nullable AS EsNullable
FROM sys.columns c
JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.MedicoPerfilExtendido')
  AND c.name = 'TipoProfesional';
GO
