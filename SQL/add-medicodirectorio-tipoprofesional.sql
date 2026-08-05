/* ============================================================================
   add-medicodirectorio-tipoprofesional.sql
   ----------------------------------------------------------------------------
   Agrega dbo.MedicosDirectorio.TipoProfesional (TINYINT NULL).

   Qué es: el dato ESTRUCTURADO que distingue médico especialista en EII de
   nutriólogo, para GUIAR qué se le sugiere validar primero en "Mis Validaciones".
       1 = MedicoEspecialistaEII
       2 = Nutriologo
       3 = Otro
       NULL = general (comportamiento actual: TOP clínico)

   Qué NO es: un permiso. El candado de validación sigue siendo el rol "Medico".
   Un nutriólogo puede validar un término clínico si quiere; esto solo ordena.

   Convive con Especialidad (NVARCHAR texto libre), que NO se toca: el tipo
   ramifica lógica, la especialidad es el detalle que se muestra.

   DEPLOY-GATE: correr ANTES de desplegar el código, porque el modelo EF ya
   espera la columna — sin ella, cualquier lectura de MedicosDirectorio revienta
   con "Invalid column name 'TipoProfesional'". Es lo contrario del caso de
   rename-nota-publicado.sql (aquel iba después).

   Idempotente: se puede re-correr sin efecto. No usa migración EF — cambio de
   esquema por SQL directo, como el resto del repo.
   ============================================================================ */
SET NOCOUNT ON;
GO

/* ---------- 1) Columna ---------- */
IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.MedicosDirectorio')
                 AND name = 'TipoProfesional')
BEGIN
    ALTER TABLE dbo.MedicosDirectorio ADD TipoProfesional TINYINT NULL;
    PRINT 'MedicosDirectorio.TipoProfesional creada.';
END
ELSE
    PRINT 'MedicosDirectorio.TipoProfesional ya existía — sin cambios.';
GO

/* ---------- 2) CHECK de dominio ----------
   Deja pasar NULL (= general). Blinda contra un 4 escrito a mano que la app
   no sabría mapear. */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints
               WHERE name = 'CK_MedicosDirectorio_TipoProfesional'
                 AND parent_object_id = OBJECT_ID('dbo.MedicosDirectorio'))
BEGIN
    ALTER TABLE dbo.MedicosDirectorio
        ADD CONSTRAINT CK_MedicosDirectorio_TipoProfesional
        CHECK (TipoProfesional IS NULL OR TipoProfesional IN (1, 2, 3));
    PRINT 'CK_MedicosDirectorio_TipoProfesional creada.';
END
ELSE
    PRINT 'CK_MedicosDirectorio_TipoProfesional ya existía — sin cambios.';
GO

/* ---------- 3) Backfill: NINGUNO a propósito ----------
   No se infiere el tipo desde Especialidad con un LIKE '%nutri%': un falso
   positivo mandaría a un médico al TOP equivocado y nadie lo notaría. Que el
   profesional o el admin lo declaren. NULL es un estado válido y seguro. */
GO

/* ---------- Verificación ---------- */
-- Debe devolver 1 fila: TipoProfesional / tinyint / is_nullable = 1
SELECT c.name AS Columna, t.name AS Tipo, c.is_nullable AS EsNullable
FROM sys.columns c
JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.MedicosDirectorio')
  AND c.name = 'TipoProfesional';

-- Distribución actual (al correrlo por primera vez: todo en NULL).
SELECT TipoProfesional, COUNT(*) AS Fichas
FROM dbo.MedicosDirectorio
GROUP BY TipoProfesional
ORDER BY TipoProfesional;
GO
