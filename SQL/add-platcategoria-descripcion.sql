-- add-platcategoria-descripcion.sql
-- Descripcion de la categoria del platillo (Entrada, Plato fuerte, Ensalada...).
-- Contenido TAXONOMICO (no clinico): solo define que agrupa la categoria. Vive en BD
-- para editarse sin recompilar. Se carga vacio; se llena desde el CRUD de Categorias
-- (boton "Generar con IA" opcional).
-- Idempotente.

IF NOT EXISTS (SELECT 1 FROM sys.columns
               WHERE object_id = OBJECT_ID('dbo.PlatCategoria') AND name = 'Descripcion')
    ALTER TABLE dbo.PlatCategoria ADD Descripcion NVARCHAR(500) NULL;
