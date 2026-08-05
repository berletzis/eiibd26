-- add-medicoperfilextendido-tipoprofesional.sql
-- Mueve TipoProfesional al perfil por-usuario (MedicoPerfilExtendido).
-- Idempotente. DEPLOY-GATE: CORRER ANTES de que el código nuevo quede vivo,
-- porque el modelo EF ya declara la columna; sin ella toda lectura de
-- MedicoPerfilExtendido truena ("Invalid column name 'TipoProfesional'").
-- Dominio: NULL = general (TOP clínico) · 1 = Médico especialista EII · 2 = Nutriólogo · 3 = Otro.

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('MedicoPerfilExtendido') AND name = 'TipoProfesional')
    ALTER TABLE MedicoPerfilExtendido ADD TipoProfesional TINYINT NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = 'CK_MedicoPerfilExtendido_TipoProfesional')
    ALTER TABLE MedicoPerfilExtendido
        ADD CONSTRAINT CK_MedicoPerfilExtendido_TipoProfesional
        CHECK (TipoProfesional IS NULL OR TipoProfesional IN (1,2,3));
GO

-- Backfill ficha -> perfil (solo si la columna vieja de MedicosDirectorio aún existe).
-- Envuelto en EXEC dinámico para que el script no truene si esa columna ya se dropeó.
IF EXISTS (SELECT 1 FROM sys.columns
           WHERE object_id = OBJECT_ID('MedicosDirectorio') AND name = 'TipoProfesional')
    EXEC('UPDATE pe SET pe.TipoProfesional = d.TipoProfesional
          FROM MedicoPerfilExtendido pe
          JOIN MedicosDirectorio d ON d.Id = pe.MedicoId
          WHERE d.TipoProfesional IS NOT NULL AND pe.TipoProfesional IS NULL;');
GO
