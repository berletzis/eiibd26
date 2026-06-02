USE eiibd26;
GO
-- Normalizar CategoriaSlug a minúscula (8 filas afectadas; el resolver ya es case-insensitive,
-- links internos y sitemap usan el slug raw → quedan consistentes en lowercase canónico)
UPDATE contenidosCategorias
SET CategoriaSlug = LOWER(CategoriaSlug)
WHERE CategoriaSlug COLLATE Latin1_General_CS_AS <> LOWER(CategoriaSlug)
  AND CategoriaSlug IS NOT NULL;

-- Normalizar Clave (campo interno admin, sin impacto público) por consistencia
UPDATE contenidosCategorias
SET Clave = LOWER(Clave)
WHERE Clave COLLATE Latin1_General_CS_AS <> LOWER(Clave)
  AND Clave IS NOT NULL;
GO
-- Verificación post-fix: debe devolver 0 filas
SELECT Sequence, Nombre, Clave, CategoriaSlug
FROM contenidosCategorias
WHERE (CategoriaSlug COLLATE Latin1_General_CS_AS <> LOWER(CategoriaSlug) AND CategoriaSlug IS NOT NULL)
   OR (Clave COLLATE Latin1_General_CS_AS <> LOWER(Clave) AND Clave IS NOT NULL);
