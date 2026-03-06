-- =============================================
-- VERIFICACIÓN DEL SISTEMA DE CALIFICACIÓN
-- =============================================

-- 1. Verificar que la tabla existe
SELECT 
    TABLE_NAME,
    TABLE_TYPE
FROM INFORMATION_SCHEMA.TABLES
WHERE TABLE_NAME = 'ArticleRatings';
GO

-- 2. Verificar estructura de columnas
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    IS_NULLABLE,
    COLUMN_DEFAULT
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'ArticleRatings'
ORDER BY ORDINAL_POSITION;
GO

-- 3. Verificar índices
SELECT 
    i.name AS IndexName,
    i.type_desc AS IndexType,
    i.is_unique AS IsUnique,
    COL_NAME(ic.object_id, ic.column_id) AS ColumnName
FROM sys.indexes i
INNER JOIN sys.index_columns ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id
WHERE i.object_id = OBJECT_ID('dbo.ArticleRatings')
ORDER BY i.name, ic.key_ordinal;
GO

-- 4. Verificar foreign keys
SELECT 
    fk.name AS ForeignKeyName,
    OBJECT_NAME(fk.parent_object_id) AS TableName,
    COL_NAME(fc.parent_object_id, fc.parent_column_id) AS ColumnName,
    OBJECT_NAME(fk.referenced_object_id) AS ReferencedTable,
    COL_NAME(fc.referenced_object_id, fc.referenced_column_id) AS ReferencedColumn,
    fk.delete_referential_action_desc AS DeleteAction
FROM sys.foreign_keys fk
INNER JOIN sys.foreign_key_columns fc ON fk.object_id = fc.constraint_object_id
WHERE fk.parent_object_id = OBJECT_ID('dbo.ArticleRatings');
GO

-- 5. Estadísticas de la tabla
SELECT 
    COUNT(*) AS TotalRatings,
    SUM(CASE WHEN RatingType = 1 THEN 1 ELSE 0 END) AS TotalLikes,
    SUM(CASE WHEN RatingType = 0 THEN 1 ELSE 0 END) AS TotalDislikes,
    COUNT(DISTINCT UserId) AS UniqueUsers,
    COUNT(DISTINCT ArticleId) AS RatedArticles
FROM dbo.ArticleRatings;
GO

-- 6. Top 5 artículos mejor calificados
SELECT TOP 5
    c.Id,
    c.ContenidoTitulo AS Titulo,
    SUM(CASE WHEN ar.RatingType = 1 THEN 1 ELSE 0 END) AS Likes,
    SUM(CASE WHEN ar.RatingType = 0 THEN 1 ELSE 0 END) AS Dislikes,
    COUNT(*) AS TotalRatings,
    CAST(SUM(CASE WHEN ar.RatingType = 1 THEN 1 ELSE 0 END) * 100.0 / COUNT(*) AS DECIMAL(5,2)) AS PositivePercentage
FROM dbo.contenidos c
INNER JOIN dbo.ArticleRatings ar ON c.Id = ar.ArticleId
WHERE c.Eliminado = 0
GROUP BY c.Id, c.ContenidoTitulo
ORDER BY PositivePercentage DESC, TotalRatings DESC;
GO

-- 7. Actividad reciente (últimos 10 votos)
SELECT TOP 10
    ar.Id,
    c.ContenidoTitulo AS Articulo,
    CASE WHEN ar.RatingType = 1 THEN 'Like' ELSE 'Dislike' END AS Tipo,
    COALESCE(u.UserName, 'Anónimo') AS Usuario,
    ar.CreatedAt,
    ar.UpdatedAt
FROM dbo.ArticleRatings ar
INNER JOIN dbo.contenidos c ON ar.ArticleId = c.Id
LEFT JOIN dbo.AspNetUsers u ON ar.UserId = u.Id
ORDER BY ar.CreatedAt DESC;
GO

PRINT '✓ Verificación completada';
