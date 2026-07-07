-- ============================================================================
-- alter-sourcesite-metadatos.sql
-- Agrega metadatos de fuente a dbo.SourceSite para el indexador multi-fuente
-- del NINA-WorkerService. Idempotente: se puede correr varias veces sin error.
-- Columnas NULL para no romper las filas existentes.
--
-- EJECUTAR ESTE SCRIPT ANTES de correr el Worker con la nueva versión multi-fuente.
-- (El Worker escribe/lee estas columnas; si no existen, fallará al guardar SourceSite.)
--
-- Base: eiibd26 (SQL Server). Cambio de esquema por SQL directo (no migraciones EF).
-- ============================================================================

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.SourceSite') AND name = 'Idioma')
    ALTER TABLE dbo.SourceSite ADD Idioma NVARCHAR(10) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.SourceSite') AND name = 'Pais')
    ALTER TABLE dbo.SourceSite ADD Pais NVARCHAR(10) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.SourceSite') AND name = 'Categoria')
    ALTER TABLE dbo.SourceSite ADD Categoria NVARCHAR(100) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.SourceSite') AND name = 'UrlPublica')
    ALTER TABLE dbo.SourceSite ADD UrlPublica NVARCHAR(500) NULL;
GO

-- Verificación (opcional): listar las columnas de SourceSite
-- SELECT COLUMN_NAME, DATA_TYPE, CHARACTER_MAXIMUM_LENGTH, IS_NULLABLE
-- FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'SourceSite' ORDER BY ORDINAL_POSITION;
