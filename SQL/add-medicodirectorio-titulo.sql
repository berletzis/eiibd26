-- add-medicodirectorio-titulo.sql
-- Título profesional del validador (Nut., Dr., Lic. en Nutrición…). Alimenta el display público
-- "Validado por": el nombre se arma como {Titulo} {NombreCompleto}. Reemplaza el "Dr." hardcodeado.
-- Nullable: si no hay título, el display muestra solo el nombre (nunca asume "Dr.").
-- Cambio de esquema por SQL directo (sin migraciones EF), como el resto del proyecto. Idempotente.

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.MedicosDirectorio') AND name = 'Titulo')
    ALTER TABLE dbo.MedicosDirectorio ADD Titulo NVARCHAR(50) NULL;
