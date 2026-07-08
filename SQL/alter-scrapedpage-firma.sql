/* =====================================================================
   Motor de Cobertura — Fase 2B: firma de externos en el Worker
   ---------------------------------------------------------------------
   Agrega a dbo.ScrapedPage las columnas donde el Worker guarda la firma
   de cobertura del artículo externo (solo la firma numérica; el texto y
   la traducción NUNCA se persisten).

   IDEMPOTENTE. Ejecuta el USUARIO. Claude Code NO corre DDL en producción.
   ===================================================================== */

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.ScrapedPage') AND name = 'Firma')
    ALTER TABLE dbo.ScrapedPage ADD Firma NVARCHAR(MAX) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.ScrapedPage') AND name = 'FirmaCalculadaEn')
    ALTER TABLE dbo.ScrapedPage ADD FirmaCalculadaEn DATETIME NULL;

-- Verificación
SELECT COUNT(*) AS ExternosConFirma FROM dbo.ScrapedPage WHERE Firma IS NOT NULL;
