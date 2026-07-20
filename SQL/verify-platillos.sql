-- verify-platillos.sql  (READ-ONLY — se corre tras cada guardado del gate)
-- Invariantes del modulo Platillos. Todo lo etiquetado [DEBE 0] tiene que dar 0.
SET NOCOUNT ON;

PRINT '=== A. CONTEOS ===';
SELECT 'platillos_total'      = (SELECT COUNT(*) FROM dbo.PlatPlatillo),
       'platillos_activos'    = (SELECT COUNT(*) FROM dbo.PlatPlatillo WHERE Activo=1),
       'renglones'            = (SELECT COUNT(*) FROM dbo.PlatPlatilloIngrediente),
       'usos'                 = (SELECT COUNT(*) FROM dbo.PlatPlatilloIngredienteAtributo),
       'ingredientes'         = (SELECT COUNT(*) FROM dbo.PlatIngrediente),
       'exclusiones_activas'  = (SELECT COUNT(*) FROM dbo.PlatPerfilExclusion WHERE Eliminado=0);

PRINT '=== B. HUERFANOS [DEBE 0] ===';
SELECT 'renglon_sin_platillo'    = (SELECT COUNT(*) FROM dbo.PlatPlatilloIngrediente pi WHERE NOT EXISTS (SELECT 1 FROM dbo.PlatPlatillo p WHERE p.Id=pi.PlatilloId)),
       'renglon_sin_ingrediente' = (SELECT COUNT(*) FROM dbo.PlatPlatilloIngrediente pi WHERE NOT EXISTS (SELECT 1 FROM dbo.PlatIngrediente i WHERE i.Id=pi.IngredienteId)),
       'uso_sin_renglon'         = (SELECT COUNT(*) FROM dbo.PlatPlatilloIngredienteAtributo a WHERE NOT EXISTS (SELECT 1 FROM dbo.PlatPlatilloIngrediente pi WHERE pi.Id=a.PlatilloIngredienteId)),
       'uso_ambito_malo'         = (SELECT COUNT(*) FROM dbo.PlatPlatilloIngredienteAtributo a WHERE NOT EXISTS (SELECT 1 FROM dbo.PlatAtributo at WHERE at.Id=a.AtributoId AND at.Ambito='Uso')),
       'intrinseco_ambito_malo'  = (SELECT COUNT(*) FROM dbo.PlatIngredienteAtributo a WHERE NOT EXISTS (SELECT 1 FROM dbo.PlatAtributo at WHERE at.Id=a.AtributoId AND at.Ambito='Ingrediente'));

PRINT '=== C. DUPLICADOS [DEBE 0 filas] ===';
SELECT 'ingrediente_dup' = LOWER(Nombre), n = COUNT(*)
FROM dbo.PlatIngrediente GROUP BY LOWER(Nombre) HAVING COUNT(*) > 1;
SELECT 'exclusion_dup_activa' = 1, idUsuario, Tipo, RefId, n = COUNT(*)
FROM dbo.PlatPerfilExclusion WHERE Eliminado=0 GROUP BY idUsuario, Tipo, RefId HAVING COUNT(*) > 1;

PRINT '=== D. SEGURIDAD: activos sin ingredientes [DEBE 0] ===';
SELECT 'activos_con_0_ingredientes' = COUNT(*)
FROM dbo.PlatPlatillo p WHERE p.Activo=1
  AND NOT EXISTS (SELECT 1 FROM dbo.PlatPlatilloIngrediente pi WHERE pi.PlatilloId=p.Id);

PRINT '=== E. SEED: renglones por platillo P001-P017 (baseline: 99 en P001-P015) ===';
SELECT p.Codigo, n = COUNT(pi.Id)
FROM dbo.PlatPlatillo p
LEFT JOIN dbo.PlatPlatilloIngrediente pi ON pi.PlatilloId=p.Id
WHERE p.Codigo LIKE 'P0[01][0-9]' AND p.Codigo <= 'P017'
GROUP BY p.Codigo ORDER BY p.Codigo;
