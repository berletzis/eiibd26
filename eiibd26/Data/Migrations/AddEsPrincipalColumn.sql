-- Migration: Add EsPrincipal column to contenidosCategoriasRelacion table
-- This column identifies the primary category for a content item (for SEO purposes)

-- Add the EsPrincipal column with default value of 0 (false)
ALTER TABLE dbo.contenidosCategoriasRelacion
ADD EsPrincipal BIT NOT NULL DEFAULT 0;
GO

-- Update existing records: mark the most recent category as primary for each content
WITH RankedCategories AS (
    SELECT 
        Sequence,
        IdContenido,
        IdCategoria,
        ROW_NUMBER() OVER (PARTITION BY IdContenido ORDER BY FechaCreacion DESC) AS RowNum
    FROM dbo.contenidosCategoriasRelacion
    WHERE Borrado = 0 AND IdCategoria IS NOT NULL
)
UPDATE ccr
SET ccr.EsPrincipal = 1,
    ccr.FechaModificacion = GETUTCDATE()
FROM dbo.contenidosCategoriasRelacion ccr
INNER JOIN RankedCategories rc ON ccr.Sequence = rc.Sequence
WHERE rc.RowNum = 1;
GO

-- Create index for better query performance
CREATE NONCLUSTERED INDEX IX_ContenidosCategoriasRelacion_EsPrincipal
ON dbo.contenidosCategoriasRelacion (IdContenido, EsPrincipal)
INCLUDE (IdCategoria, Borrado)
WHERE EsPrincipal = 1 AND Borrado = 0;
GO
