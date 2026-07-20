/* ============================================================================
   EXPORTAR ARTÍCULOS DE LA CATEGORÍA "Soporte Técnico" (manuales de ayuda)
   SOLO LECTURA — únicamente SELECT. No modifica contenidos ni esquema.

   CÓMO EJECUTARLO (volcado a archivo sin truncar el cuerpo nvarchar(max)):

     sqlcmd -S <servidor> -d <bd> -E -f 65001 -y 0 -h -1 ^
            -i SQL/export-contenidos-soporte-tecnico.sql ^
            -o contenidos-soporte-tecnico.txt

   Flags clave:
     -f 65001  salida en UTF-8 (conserva acentos y los caracteres ═ ─)
     -y 0      ancho de columna ILIMITADO  ← evita truncar el cuerpo HTML
     -h -1     sin encabezados de columna
     -E        autenticación integrada de Windows
               (Azure SQL / auth SQL: usar  -U <usuario> -P <password>  en vez de -E)

   NOTA: SSMS "Results to Text" trunca nvarchar(max) (~64 KB). Usar sqlcmd
   para garantizar el cuerpo completo, como pide la tarea.
   ============================================================================ */

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;   -- requerido por los métodos XML (.value) usados en el agregado de categorías

/* ---------------------------------------------------------------------------
   (OPCIONAL) DESCUBRIMIENTO: confirmar el nombre/Sequence exacto de la categoría
   y cuántos artículos tiene. Descomentar para verificar antes de exportar.

   SELECT c.Sequence, c.Nombre, c.CategoriaSlug,
          COUNT(DISTINCT r.IdContenido) AS Articulos
   FROM dbo.contenidosCategorias c
   LEFT JOIN dbo.contenidosCategoriasRelacion r
          ON r.IdCategoria = c.Sequence AND r.Borrado = 0
   WHERE c.Nombre LIKE N'%Soporte%'
   GROUP BY c.Sequence, c.Nombre, c.CategoriaSlug;
   --------------------------------------------------------------------------- */

DECLARE @nl   nchar(2)     = NCHAR(13) + NCHAR(10);
DECLARE @eq   nvarchar(60) = REPLICATE(N'═', 47);
DECLARE @dash nvarchar(60) = REPLICATE(N'─', 47);

;WITH cats AS (
    -- Todas las categorías (no borradas) de cada contenido, para la línea CATEGORÍAS.
    -- FOR XML PATH en vez de STRING_AGG para compatibilidad con cualquier versión de SQL Server.
    SELECT r.IdContenido,
           STUFF((
               SELECT N', ' + c2.Nombre
               FROM dbo.contenidosCategoriasRelacion r2
               JOIN dbo.contenidosCategorias c2
                    ON c2.Sequence = r2.IdCategoria AND c2.Borrado = 0
               WHERE r2.IdContenido = r.IdContenido AND r2.Borrado = 0
               ORDER BY c2.Nombre
               FOR XML PATH(''), TYPE).value('.', 'nvarchar(max)'), 1, 2, N'') AS Categorias
    FROM dbo.contenidosCategoriasRelacion r
    WHERE r.Borrado = 0
    GROUP BY r.IdContenido
)
SELECT
    @eq + @nl +
    N'ID: '          + CAST(co.Id AS nvarchar(20))                       + @nl +
    N'TÍTULO: '      + ISNULL(co.ContenidoTitulo, N'')                   + @nl +
    N'SLUG: '        + ISNULL(co.ContenidoTituloSlug, N'')               + @nl +
    N'CATEGORÍAS: '  + ISNULL(ct.Categorias, N'')                        + @nl +
    N'ESTADO: '      + CASE WHEN co.EstadoPublicacion = 1 THEN N'Publicado'
                           WHEN co.EstadoPublicacion = 0 THEN N'Borrador'
                           ELSE N'(' + ISNULL(CAST(co.EstadoPublicacion AS nvarchar(10)), N'null') + N')'
                      END                                                + @nl +
    N'FECHA: '       + CONVERT(nvarchar(30), co.FechaCreado, 120)        + @nl +
    @dash + @nl +
    N'RESUMEN:' + @nl +
    ISNULL(co.ContenidoTextoC, N'')                                      + @nl +
    @dash + @nl +
    N'CUERPO:' + @nl +
    ISNULL(co.ContenidoTextoL, N'')                                      + @nl +   -- cuerpo HTML completo, tal cual
    @eq AS Bloque
FROM dbo.contenidos co
LEFT JOIN cats ct ON ct.IdContenido = co.Id
WHERE co.Eliminado = 0
  AND EXISTS (
        SELECT 1
        FROM dbo.contenidosCategoriasRelacion r
        JOIN dbo.contenidosCategorias c
             ON c.Sequence = r.IdCategoria AND c.Borrado = 0
        WHERE r.IdContenido = co.Id
          AND r.Borrado = 0
          AND c.Nombre LIKE N'Soporte T_cnico'   -- '_' casa 'é' o 'e' (acento-robusto)
  )
ORDER BY co.Id;
