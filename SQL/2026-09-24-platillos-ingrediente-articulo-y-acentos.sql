-- 2026-09-24-platillos-ingrediente-articulo-y-acentos.sql
--
-- Contexto: la encuesta /tolero/{slug} decía "¿Toleras el leche?" (artículo fijo "el") y el
-- catálogo tenía nombres sin acento ("azucar", "platano"). Tres partes, todas idempotentes:
--
--   1. Columna PlatIngrediente.Articulo (el/la/los/las, NULL = sin artículo → la vista usa una
--      frase neutra). DEPLOY-GATE: correr ANTES de desplegar el código. EF mapea la columna en la
--      entidad PlatIngrediente; sin ella, toda consulta que materialice la entidad truena con
--      "Invalid column name 'Articulo'".
--   2. Acentos en 3 ingredientes y 1 grupo. Es SEGURO para URLs e íconos: SlugHelper quita los
--      acentos, así que /tolero/azucar, /Platillos/Ingrediente/azucar, el SVG del ícono y la clase
--      de color del grupo resuelven exactamente igual antes y después.
--   3. Artículo de los 79 ingredientes activos al 24 SEP 2026. Solo llena los NULL: no pisa lo que
--      el admin ya haya capturado. Los que no estén en la lista quedan NULL (frase neutra).
--
-- Correr con: sqlcmd ... -f 65001 -i <este archivo>   (sin -f 65001 los acentos entran como mojibake)

SET NOCOUNT ON;

-- ── 1. Columna ──────────────────────────────────────────────────────────────────────────────
IF NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('dbo.PlatIngrediente')
               AND name = 'Articulo')
    ALTER TABLE dbo.PlatIngrediente ADD Articulo NVARCHAR(3) NULL;
GO

IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PlatIngrediente_Articulo')
    ALTER TABLE dbo.PlatIngrediente ADD CONSTRAINT CK_PlatIngrediente_Articulo
        CHECK (Articulo IS NULL OR Articulo IN (N'el', N'la', N'los', N'las'));
GO

-- ── 2. Acentos ──────────────────────────────────────────────────────────────────────────────
-- Solo si el nombre destino no existe ya (UQ_PlatIngrediente_Nombre / UQ_PlatGrupo_Nombre).
UPDATE i SET i.Nombre = v.Nuevo
FROM dbo.PlatIngrediente i
JOIN (VALUES (N'azucar', N'azúcar'),
             (N'endulzante liquido', N'endulzante líquido'),
             (N'platano', N'plátano')) v(Viejo, Nuevo)
  ON i.Nombre = v.Viejo COLLATE Modern_Spanish_CS_AS
WHERE NOT EXISTS (SELECT 1 FROM dbo.PlatIngrediente x WHERE x.Nombre = v.Nuevo COLLATE Modern_Spanish_CS_AS);

UPDATE g SET g.Nombre = N'Endulzante / Azúcares'
FROM dbo.PlatGrupo g
WHERE g.Nombre = N'Endulzante / Azucares' COLLATE Modern_Spanish_CS_AS
  AND NOT EXISTS (SELECT 1 FROM dbo.PlatGrupo x WHERE x.Nombre = N'Endulzante / Azúcares' COLLATE Modern_Spanish_CS_AS);
GO

-- ── 3. Artículos ────────────────────────────────────────────────────────────────────────────
UPDATE i SET i.Articulo = v.Articulo
FROM dbo.PlatIngrediente i
JOIN (VALUES
    (N'azúcar', N'el'), (N'endulzante líquido', N'el'),
    (N'crema', N'la'), (N'leche', N'la'), (N'queso', N'el'), (N'yogur', N'el'),
    (N'huevo', N'el'), (N'pollo', N'el'), (N'jamón de pavo', N'el'), (N'tocino', N'el'),
    (N'atún', N'el'), (N'pescado blanco', N'el'), (N'camarón', N'el'),
    (N'acelga', N'la'), (N'apio', N'el'), (N'betabel', N'el'), (N'cebolla', N'la'),
    (N'cebollín', N'el'), (N'col', N'la'), (N'coliflor', N'la'), (N'espinaca', N'la'),
    (N'jalapeño', N'el'), (N'jitomate', N'el'), (N'lechuga', N'la'), (N'pepino', N'el'),
    (N'tomate', N'el'), (N'zanahoria', N'la'), (N'zapallito italiano', N'el'),
    (N'aguacate', N'el'), (N'durazno', N'el'), (N'frambuesa', N'la'), (N'fresa', N'la'),
    (N'kiwi', N'el'), (N'limón', N'el'), (N'manzana', N'la'), (N'melón', N'el'),
    (N'naranja', N'la'), (N'papaya', N'la'), (N'pera', N'la'), (N'piña', N'la'),
    (N'plátano', N'el'), (N'pulpa de durazno', N'la'),
    (N'almendra', N'la'), (N'cacahuate', N'el'), (N'crema de cacahuate', N'la'),
    (N'amaranto', N'el'), (N'arroz', N'el'), (N'avena', N'la'), (N'brioche', N'el'),
    (N'espagueti', N'el'), (N'granola', N'la'), (N'granos de elote', N'los'),
    (N'harina de trigo', N'la'), (N'pan integral', N'el'), (N'pan pita', N'el'),
    (N'tortilla de maíz', N'la'), (N'tostada de maíz', N'la'),
    (N'arveja', N'la'), (N'papa', N'la'), (N'champiñón', N'el'),
    (N'aceite', N'el'), (N'aceite de oliva', N'el'), (N'mayonesa', N'la'),
    (N'albahaca', N'la'), (N'chiltepín', N'el'), (N'cilantro', N'el'), (N'jengibre', N'el'),
    (N'laurel', N'el'), (N'pimienta', N'la'), (N'sal', N'la'), (N'salsa de soya', N'la'),
    (N'agua', N'el'), (N'café', N'el'), (N'cerveza', N'la'), (N'hielo', N'el'),
    (N'leche de coco', N'la'), (N'caldo de pollo', N'el'), (N'chía', N'la'),
    (N'gelatina natural', N'la')
) v(Nombre, Articulo)
  ON i.Nombre = v.Nombre COLLATE Modern_Spanish_CS_AS
WHERE i.Articulo IS NULL;
GO

-- ── Verificación ────────────────────────────────────────────────────────────────────────────
-- Esperado: 0 filas con los nombres viejos; ActivosSinArticulo = 0 (salvo ingredientes dados de
-- alta después del 24 SEP, que aparecen listados abajo para capturarles el artículo en el admin).
SELECT Nombre AS NombreViejoPendiente FROM dbo.PlatIngrediente
WHERE Nombre COLLATE Modern_Spanish_CS_AS IN (N'azucar', N'endulzante liquido', N'platano');
SELECT Nombre AS GrupoViejoPendiente FROM dbo.PlatGrupo
WHERE Nombre COLLATE Modern_Spanish_CS_AS = N'Endulzante / Azucares';
SELECT COUNT(*) AS ActivosSinArticulo FROM dbo.PlatIngrediente WHERE Activo = 1 AND Articulo IS NULL;
SELECT Id, Nombre FROM dbo.PlatIngrediente WHERE Activo = 1 AND Articulo IS NULL ORDER BY Nombre;
SELECT Articulo, COUNT(*) AS N FROM dbo.PlatIngrediente WHERE Activo = 1 GROUP BY Articulo;
