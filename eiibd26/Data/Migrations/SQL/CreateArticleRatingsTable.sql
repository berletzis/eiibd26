-- =============================================
-- Script: Crear tabla ArticleRatings
-- Descripción: Sistema de calificación Like/Dislike para artículos
-- Fecha: 2024
-- =============================================

-- Crear la tabla ArticleRatings
CREATE TABLE [dbo].[ArticleRatings] (
    [Id] INT IDENTITY(1,1) NOT NULL,
    [ArticleId] INT NOT NULL,
    [RatingType] INT NOT NULL, -- 0 = Dislike, 1 = Like
    [UserId] UNIQUEIDENTIFIER NULL,
    [IpAddress] NVARCHAR(45) NULL,
    [CreatedAt] DATETIME2(7) NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2(7) NULL,

    CONSTRAINT [PK_ArticleRatings] PRIMARY KEY CLUSTERED ([Id] ASC),

    -- Foreign Key con Contenidos (Articles)
    CONSTRAINT [FK_ArticleRatings_Contenidos_ArticleId] 
        FOREIGN KEY ([ArticleId]) 
        REFERENCES [dbo].[contenidos] ([Id]) 
        ON DELETE CASCADE,

    -- Foreign Key con AspNetUsers (opcional)
    CONSTRAINT [FK_ArticleRatings_AspNetUsers_UserId] 
        FOREIGN KEY ([UserId]) 
        REFERENCES [dbo].[AspNetUsers] ([Id]) 
        ON DELETE SET NULL
);
GO

-- =============================================
-- ÍNDICES PARA OPTIMIZACIÓN
-- =============================================

-- Índice para búsquedas por artículo
CREATE NONCLUSTERED INDEX [IX_ArticleRatings_ArticleId] 
ON [dbo].[ArticleRatings] ([ArticleId] ASC);
GO

-- Índice para búsquedas por usuario
CREATE NONCLUSTERED INDEX [IX_ArticleRatings_UserId] 
ON [dbo].[ArticleRatings] ([UserId] ASC);
GO

-- =============================================
-- RESTRICCIÓN ÚNICA: Un voto por usuario por artículo
-- =============================================

-- Para usuarios autenticados: UserId + ArticleId deben ser únicos
CREATE UNIQUE NONCLUSTERED INDEX [IX_ArticleRatings_UserId_ArticleId] 
ON [dbo].[ArticleRatings] ([UserId] ASC, [ArticleId] ASC)
WHERE [UserId] IS NOT NULL;
GO

-- =============================================
-- ÍNDICE OPCIONAL: Para usuarios anónimos por IP
-- (Comentado por defecto, descomentar si se desea usar)
-- =============================================

/*
CREATE UNIQUE NONCLUSTERED INDEX [IX_ArticleRatings_IpAddress_ArticleId] 
ON [dbo].[ArticleRatings] ([IpAddress] ASC, [ArticleId] ASC)
WHERE [IpAddress] IS NOT NULL AND [UserId] IS NULL;
GO
*/

-- =============================================
-- VERIFICACIÓN
-- =============================================

-- Verificar que la tabla se creó correctamente
SELECT 
    OBJECT_NAME(object_id) AS TableName,
    name AS IndexName,
    type_desc AS IndexType,
    is_unique AS IsUnique
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.ArticleRatings')
ORDER BY index_id;
GO

-- Verificar constraints
SELECT 
    OBJECT_NAME(parent_object_id) AS TableName,
    name AS ConstraintName,
    type_desc AS ConstraintType
FROM sys.foreign_keys
WHERE parent_object_id = OBJECT_ID('dbo.ArticleRatings');
GO

PRINT 'Tabla ArticleRatings creada exitosamente ✓';
PRINT 'Índices creados exitosamente ✓';
PRINT 'Restricciones aplicadas exitosamente ✓';
GO
