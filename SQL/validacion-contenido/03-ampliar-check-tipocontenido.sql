-- ============================================================
-- 03 - Ampliar CK_VCP_TipoContenido para admitir nuevos tipos
-- ------------------------------------------------------------
-- El CHECK original limitaba TipoContenido a IN (1, 2) (Termino, Articulo).
-- El enum ya creció: 3 = PerfilMedico, 4 = NotaClinicaIngrediente (notas
-- clínicas de Platillos, F2b). Sin ampliar el CHECK, guardar una validación
-- de nota (tipo 4) lanza excepción y el handler devuelve "Error al guardar".
--
-- Drop + recreate (no ALTER in-place): así funciona sea cual sea la definición
-- actual del constraint en prod. WITH CHECK valida las filas existentes (todas
-- 1/2/3), que ya cumplen. Idempotente: re-correrlo solo lo vuelve a dejar igual.
-- Cambio de esquema por SQL directo (sin migración EF), como el resto del repo.
-- ============================================================
SET NOCOUNT ON;
GO

IF EXISTS (SELECT 1 FROM sys.check_constraints
           WHERE name = 'CK_VCP_TipoContenido'
             AND parent_object_id = OBJECT_ID('dbo.ValidacionesContenidoProfesional'))
BEGIN
    ALTER TABLE dbo.ValidacionesContenidoProfesional DROP CONSTRAINT CK_VCP_TipoContenido;
    PRINT 'CK_VCP_TipoContenido anterior eliminado.';
END
GO

ALTER TABLE dbo.ValidacionesContenidoProfesional WITH CHECK
    ADD CONSTRAINT CK_VCP_TipoContenido
        CHECK (TipoContenido IN (1, 2, 3, 4));  -- 1 Termino, 2 Articulo, 3 PerfilMedico, 4 NotaClinicaIngrediente
GO

PRINT 'CK_VCP_TipoContenido ampliado a (1, 2, 3, 4).';

-- Verificación: debe listar el constraint con la definición nueva.
SELECT name, definition
FROM sys.check_constraints
WHERE name = 'CK_VCP_TipoContenido'
  AND parent_object_id = OBJECT_ID('dbo.ValidacionesContenidoProfesional');
GO
