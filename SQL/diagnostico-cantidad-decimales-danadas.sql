/* =============================================================================
   DIAGNÓSTICO (SOLO LECTURA) — Cantidades de ingrediente dañadas por el bug de
   localización del model binding (0.25 se guardaba como 25).

   Contexto: <input type=number> postea siempre con punto (0.25), pero el binder
   del framework parseaba con la cultura de la request; bajo es-ES/es el punto es
   separador de MILES → N/100 quedó como N. Corregido en código con
   eiibd26/ModelBinding/InvariantNumberModelBinder.cs (afecta capturas NUEVAS).

   ESTE SCRIPT NO MODIFICA NADA. Son SELECTs. No hay UPDATE aquí a propósito:
   dividir /100 a ciegas destruiría cantidades legítimamente enteras (25 g, 100 ml).
   La corrección de los renglones dañados requiere autorización explícita y se
   decide caso por caso con la evidencia de TextoOriginal.
   ============================================================================= */

/* -----------------------------------------------------------------------------
   1) SOSPECHOSOS FUERTES — TextoOriginal trae una fracción ("1/4 taza") pero la
      Cantidad guardada es un entero. Es la firma exacta del bug.
   ----------------------------------------------------------------------------- */
SELECT
    pi.Id                AS RenglonId,
    p.Id                 AS PlatilloId,
    p.Codigo,
    p.Nombre             AS Platillo,
    ing.Nombre           AS Ingrediente,
    pi.TextoOriginal,
    pi.Cantidad          AS CantidadActual,
    pi.Cantidad / 100.0  AS CantidadSiFuera_N_entre_100,
    u.Nombre             AS Unidad
FROM dbo.PlatPlatilloIngrediente pi
JOIN dbo.PlatPlatillo    p   ON p.Id   = pi.PlatilloId
LEFT JOIN dbo.PlatIngrediente ing ON ing.Id = pi.IngredienteId
LEFT JOIN dbo.PlatUnidad u   ON u.Id   = pi.UnidadId
WHERE pi.Cantidad IS NOT NULL
  AND pi.Cantidad = FLOOR(pi.Cantidad)          -- se guardó como entero
  AND pi.TextoOriginal LIKE '%/%'               -- pero el texto fuente trae fracción
ORDER BY p.Codigo, pi.Id;

/* -----------------------------------------------------------------------------
   2) SOSPECHOSOS POR MAGNITUD — cantidades enteras "raras" para su unidad.
      Un 25 en "taza" o "cucharada" casi seguro era 0.25; un 25 en "g" es legítimo.
      Revisar a mano contra TextoOriginal antes de tocar nada.
   ----------------------------------------------------------------------------- */
SELECT
    pi.Id                AS RenglonId,
    p.Codigo,
    p.Nombre             AS Platillo,
    ing.Nombre           AS Ingrediente,
    pi.TextoOriginal,
    pi.Cantidad          AS CantidadActual,
    u.Nombre             AS Unidad
FROM dbo.PlatPlatilloIngrediente pi
JOIN dbo.PlatPlatillo    p   ON p.Id   = pi.PlatilloId
LEFT JOIN dbo.PlatIngrediente ing ON ing.Id = pi.IngredienteId
LEFT JOIN dbo.PlatUnidad u   ON u.Id   = pi.UnidadId
WHERE pi.Cantidad IS NOT NULL
  AND pi.Cantidad = FLOOR(pi.Cantidad)
  AND pi.Cantidad >= 10                          -- 10, 25, 33, 5 0, 75 ... típicos de N/100
  AND u.Nombre IN (N'taza', N'tazas', N'cucharada', N'cucharadas',
                   N'cucharadita', N'cucharaditas', N'pizca', N'pieza', N'piezas')
ORDER BY pi.Cantidad DESC, p.Codigo;

/* -----------------------------------------------------------------------------
   3) CONTEO GLOBAL — para dimensionar el daño antes de decidir estrategia.
   ----------------------------------------------------------------------------- */
SELECT
    COUNT(*)                                                            AS TotalRenglonesConCantidad,
    SUM(CASE WHEN pi.Cantidad <> FLOOR(pi.Cantidad) THEN 1 ELSE 0 END)  AS ConDecimalesOk,
    SUM(CASE WHEN pi.Cantidad =  FLOOR(pi.Cantidad) THEN 1 ELSE 0 END)  AS Enteros,
    SUM(CASE WHEN pi.Cantidad =  FLOOR(pi.Cantidad)
              AND pi.TextoOriginal LIKE '%/%'       THEN 1 ELSE 0 END)  AS SospechososFuertes
FROM dbo.PlatPlatilloIngrediente pi
WHERE pi.Cantidad IS NOT NULL;
