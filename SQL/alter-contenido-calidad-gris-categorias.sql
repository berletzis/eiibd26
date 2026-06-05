-- GRIS: análisis de categorías — sugerencias + alertas de mala clasificación
-- Ejecutar en producción (idempotente)

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.ContenidoCalidad') AND name = 'GrisCategoriasSugeridas')
    ALTER TABLE dbo.ContenidoCalidad ADD GrisCategoriasSugeridas NVARCHAR(MAX) NULL;
-- JSON: [{categoriaId, nombre, razon}] — categorías donde encajaría el artículo

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.ContenidoCalidad') AND name = 'GrisCategoriasAlerta')
    ALTER TABLE dbo.ContenidoCalidad ADD GrisCategoriasAlerta NVARCHAR(MAX) NULL;
-- JSON: [{categoriaId, nombre, razon}] — categorías actuales que no corresponden al contenido
